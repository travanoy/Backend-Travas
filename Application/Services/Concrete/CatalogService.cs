using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class CatalogService(
    IAnimeRepository animeRepository,
    IProgressRepository progressRepository,
    IShikimoriClient shikimoriClient,
    IKodikClient kodikClient,
    IPushNotificationService pushNotificationService,
    INotificationRepository notificationRepository,
    ILogger<CatalogService> logger) : ICatalogService
{
    public async Task SyncCatalogAsync(CancellationToken cancellationToken = default)
    {
        var popular = await shikimoriClient.GetPopularAsync(100, cancellationToken);
        await Task.Delay(250, cancellationToken);
        var ongoing = await shikimoriClient.GetOngoingAsync(100, cancellationToken);

        var map = new Dictionary<int, ShikimoriAnimeData>();

        foreach (var anime in popular.Concat(ongoing))
            map[anime.Id] = anime;

        if (map.Count == 0)
        {
            logger.LogWarning("Catalog sync finished: no data returned by Shikimori");
            return;
        }

        // Сохраняем пачками, чтобы не держать один огромный ChangeTracker.
        foreach (var chunk in map.Values.Chunk(50))
        {
            await animeRepository.UpsertManyAsync(chunk.Select(AnimeMapper.FromShikimori), cancellationToken);
            await Task.Delay(200, cancellationToken);
        }

        logger.LogInformation("Catalog sync finished: {Count} anime processed", map.Count);
    }

    public async Task CheckNewEpisodesAsync(CancellationToken cancellationToken = default)
    {
        var updated = await kodikClient.GetRecentlyUpdatedAsync(30, cancellationToken);
        if (updated.Count == 0)
            return;

        foreach (var item in updated)
        {
            try
            {
                await ProcessUpdatedItemAsync(item, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process updated anime {ShikimoriId}", item.ShikimoriId);
            }
        }
    }

    private async Task ProcessUpdatedItemAsync(KodikUpdatedItem item, CancellationToken cancellationToken)
    {
        if (item.LastEpisode is null or <= 0)
            return;

        var anime = await animeRepository.GetByShikimoriIdAsync(item.ShikimoriId, cancellationToken);
        if (anime is null)
            return;

        var knownEpisodes = anime.Episodes ?? 0;
        if (item.LastEpisode.Value <= knownEpisodes)
            return;

        anime.EpisodesAired = item.LastEpisode.Value;
        anime.UpdatedAt = DateTime.UtcNow;
        await animeRepository.UpdateAsync(anime, cancellationToken);

        var subscribers = await progressRepository.GetPushSubscribersAsync(item.ShikimoriId, cancellationToken);
        var tokens = subscribers
            .Select(user => user.ExpoPushToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => token!)
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
        {
            logger.LogInformation("Anime {ShikimoriId} got episode {Episode}, but no push subscribers", item.ShikimoriId, item.LastEpisode);
            return;
        }

        await pushNotificationService.SendAsync(
            tokens,
            "New episode",
            $"{anime.Title} — episode {item.LastEpisode} is available",
            new { id = anime.Id, shikimoriId = item.ShikimoriId },
            cancellationToken);

        logger.LogInformation(
            "Pushed episode {Episode} of anime {ShikimoriId} to {Count} devices",
            item.LastEpisode,
            item.ShikimoriId,
            tokens.Count);
    }

    public async Task CheckNewEpisodesFromShikimoriAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[] { WatchingEnum.Watching, WatchingEnum.Planned };
        var shikimoriIds = await progressRepository.GetTrackedShikimoriIdsAsync(statuses, cancellationToken);

        if (shikimoriIds.Count == 0)
        {
            logger.LogInformation("Shikimori episode check: no tracked anime");
            return;
        }

        foreach (var chunk in shikimoriIds.Chunk(50))
        {
            var external = await shikimoriClient.GetByIdsAsync(chunk, cancellationToken);
            if (external.Count == 0)
                continue;

            var local = await animeRepository.GetByShikimoriIdsAsync(chunk, cancellationToken);
            var localByShikimori = local
                .Where(anime => anime.ShikimoriId is > 0)
                .ToDictionary(anime => anime.ShikimoriId!.Value);

            foreach (var data in external)
            {
                if (!localByShikimori.TryGetValue(data.Id, out var anime))
                    continue;

                var aired = data.EpisodesAired ?? 0;
                if (aired <= 0 || aired <= anime.EpisodesAired)
                    continue;

                var previousAired = anime.EpisodesAired;

                // Первый раз видим это аниме — просто запоминаем базу, без пуша.
                var isBaseline = previousAired <= 0;

                anime.EpisodesAired = aired;
                if (data.Episodes is > 0)
                    anime.Episodes = data.Episodes.Value;
                anime.NextEpisodeAt = data.NextEpisodeAt;

                if (data.Episodes is > 0 && aired >= data.Episodes.Value)
                    anime.Status = AnimeStatus.FinishedAiring;

                anime.UpdatedAt = DateTime.UtcNow;
                await animeRepository.UpdateAsync(anime, cancellationToken);

                if (isBaseline)
                {
                    logger.LogInformation("Episode baseline for {ShikimoriId}: {Aired}", data.Id, aired);
                    continue;
                }

                await NotifyNewEpisodeAsync(anime, previousAired, aired, cancellationToken);
            }
        }
    }

    private async Task NotifyNewEpisodeAsync(Anime anime, int previousAired, int aired, CancellationToken cancellationToken)
    {
        var shikimoriId = anime.ShikimoriId ?? 0;
        var subscribers = await progressRepository.GetPushSubscribersAsync(shikimoriId, cancellationToken);

        if (subscribers.Count == 0)
        {
            logger.LogInformation("Episode {Episode} of {ShikimoriId} — no subscribers", aired, shikimoriId);
            return;
        }

        // Лента уведомлений: пишем в БД по каждому пользователю, у кого аниме в списке.
        await CreateFeedNotificationsAsync(subscribers, anime, previousAired, aired, cancellationToken);

        var tokens = subscribers
            .Select(user => user.ExpoPushToken)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Select(token => token!)
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
        {
            logger.LogInformation("Episode {Episode} of {ShikimoriId} — feed saved, but no push tokens", aired, shikimoriId);
            return;
        }

        await pushNotificationService.SendAsync(
            tokens,
            "New episode",
            $"{anime.Title} — episode {aired} is available",
            new { id = anime.Id, shikimoriId },
            cancellationToken);

        logger.LogInformation(
            "Pushed episode {Episode} of {ShikimoriId} to {Count} devices",
            aired,
            shikimoriId,
            tokens.Count);
    }

    private async Task CreateFeedNotificationsAsync(
        IReadOnlyList<User> subscribers,
        Anime anime,
        int previousAired,
        int aired,
        CancellationToken cancellationToken)
    {
        var pending = new List<EpisodeNotification>();

        for (var episode = previousAired + 1; episode <= aired; episode++)
        {
            foreach (var user in subscribers)
            {
                if (await notificationRepository.ExistsAsync(user.Id, anime.Id, episode, cancellationToken))
                    continue;

                pending.Add(new EpisodeNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    AnimeId = anime.Id,
                    Episode = episode,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (pending.Count > 0)
            await notificationRepository.AddRangeAsync(pending, cancellationToken);
    }
}
