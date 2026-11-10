using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class AnimeService(
    IAnimeRepository animeRepository,
    IProgressRepository progressRepository,
    IShikimoriClient shikimoriClient,
    ILogger<AnimeService> logger) : IAnimeService
{
    private static readonly string[] Statuses = ["Ongoing", "Completed", "Upcoming"];

    public async Task<Result<PagedResult<AnimeWithProgress>>> SearchAsync(
        string? search,
        string? genre,
        string? status,
        int page,
        int pageSize,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        AnimeStatus? airingStatus = null;
        if (AiringStatusLabel.TryParse(status, out var parsedStatus))
            airingStatus = parsedStatus;

        var normalizedSearch = NormalizeSearch(search);
        var animes = await animeRepository.SearchAsync(normalizedSearch, genre, airingStatus, cancellationToken);

        
        if (animes.Count == 0 && normalizedSearch is not null)
        {
            var external = await shikimoriClient.SearchAsync(normalizedSearch, cancellationToken);
            if (external.Count > 0)
            {
                await animeRepository.UpsertManyAsync(external.Select(AnimeMapper.FromShikimori), cancellationToken);
                logger.LogInformation("Read-through imported {Count} anime for search {Search}", external.Count, normalizedSearch);
                animes = await animeRepository.SearchAsync(normalizedSearch, genre, airingStatus, cancellationToken);
            }
        }

        var progressByAnime = await GetProgressMapAsync(userId, cancellationToken);

        var items = animes
            .Select(anime => new AnimeWithProgress
            {
                Anime = anime,
                Progress = progressByAnime.GetValueOrDefault(anime.Id)
            })
            .ToList();

        return Result<PagedResult<AnimeWithProgress>>.Success(ToPage(items, page, pageSize));
    }

    public async Task<Result<AnimeWithProgress>> GetByIdAsync(
        Guid id,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var anime = await animeRepository.GetByIdAsync(id, cancellationToken);
        if (anime is null)
            return Result<AnimeWithProgress>.Failure("Anime not found.", ErrorType.NotFound);

        var progress = userId is null
            ? null
            : await progressRepository.GetAsync(userId.Value, anime.Id, cancellationToken);

        return Result<AnimeWithProgress>.Success(new AnimeWithProgress { Anime = anime, Progress = progress });
    }

    public async Task<Result<AnimeWithProgress>> GetByShikimoriIdAsync(
        int shikimoriId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var anime = await animeRepository.GetByShikimoriIdAsync(shikimoriId, cancellationToken);

        if (anime is null)
        {
            var external = await shikimoriClient.GetByIdAsync(shikimoriId, cancellationToken);
            if (external is null)
                return Result<AnimeWithProgress>.Failure("Anime not found.", ErrorType.NotFound);

            anime = AnimeMapper.FromShikimori(external);
            await animeRepository.AddAsync(anime, cancellationToken);
        }

        var progress = userId is null
            ? null
            : await progressRepository.GetAsync(userId.Value, anime.Id, cancellationToken);

        return Result<AnimeWithProgress>.Success(new AnimeWithProgress { Anime = anime, Progress = progress });
    }

    public async Task<Result<Anime>> GetBannerAsync(CancellationToken cancellationToken = default)
    {
        var anime = await animeRepository.GetBannerAsync(cancellationToken);

        if (anime is null)
        {
            var popular = await shikimoriClient.GetPopularAsync(1, cancellationToken);
            if (popular.Count == 0)
                return Result<Anime>.Failure("Catalog is empty.", ErrorType.NotFound);

            var imported = popular.Select(AnimeMapper.FromShikimori).ToList();
            await animeRepository.UpsertManyAsync(imported, cancellationToken);
            anime = await animeRepository.GetBannerAsync(cancellationToken);
        }

        return anime is null
            ? Result<Anime>.Failure("Catalog is empty.", ErrorType.NotFound)
            : Result<Anime>.Success(anime);
    }

    public async Task<Result<IReadOnlyList<string>>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        var genres = await animeRepository.GetGenresAsync(cancellationToken);
        return Result<IReadOnlyList<string>>.Success(genres);
    }

    public IReadOnlyList<string> GetStatuses() => Statuses;

    public async Task<Result<IReadOnlyList<ContinueWatchingItem>>> GetContinueWatchingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var items = await progressRepository.GetContinueWatchingAsync(userId, cancellationToken);

        return Result<IReadOnlyList<ContinueWatchingItem>>.Success(items
            .Where(item => item.Anime is not null)
            .Select(item => new ContinueWatchingItem { Anime = item.Anime, Progress = item })
            .ToList());
    }

    public async Task<Result<IReadOnlyList<AnimeWithProgress>>> GetNewEpisodesAsync(
        Guid? userId,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var animes = await animeRepository.GetNewEpisodesAsync(limit, cancellationToken);
        var progressByAnime = await GetProgressMapAsync(userId, cancellationToken);

        return Result<IReadOnlyList<AnimeWithProgress>>.Success(animes
            .Select(anime => new AnimeWithProgress
            {
                Anime = anime,
                Progress = progressByAnime.GetValueOrDefault(anime.Id)
            })
            .ToList());
    }

    public async Task<Result<PagedResult<AnimeWithProgress>>> GetSavedAsync(
        Guid userId,
        string? statusSlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        WatchingEnum? status = null;
        if (!string.IsNullOrWhiteSpace(statusSlug))
        {
            if (!WatchStatusSlug.TryParse(statusSlug, out var parsed))
                return Result<PagedResult<AnimeWithProgress>>.Failure("Unknown watch status.", ErrorType.Validation);

            status = parsed;
        }

        var progresses = await progressRepository.GetByUserAsync(userId, status, cancellationToken);

        var items = progresses
            .Where(progress => progress.Anime is not null)
            .Select(progress => new AnimeWithProgress { Anime = progress.Anime, Progress = progress })
            .ToList();

        return Result<PagedResult<AnimeWithProgress>>.Success(ToPage(items, page, pageSize));
    }

    private static PagedResult<AnimeWithProgress> ToPage(IReadOnlyList<AnimeWithProgress> items, int page, int pageSize)
    {
        var totalCount = items.Count;
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<AnimeWithProgress>
        {
            Items = paged,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<Dictionary<Guid, UserAnimeProgress>> GetProgressMapAsync(
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null)
            return new Dictionary<Guid, UserAnimeProgress>();

        var progresses = await progressRepository.GetAllByUserAsync(userId.Value, cancellationToken);
        return progresses.ToDictionary(progress => progress.AnimeId);
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
