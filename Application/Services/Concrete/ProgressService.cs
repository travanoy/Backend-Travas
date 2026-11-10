using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class ProgressService(
    IAnimeRepository animeRepository,
    IProgressRepository progressRepository,
    ILogger<ProgressService> logger) : IProgressService
{
    public async Task<Result<AnimeWithProgress>> SetWatchStatusAsync(
        Guid userId,
        Guid animeId,
        string statusSlug,
        CancellationToken cancellationToken = default)
    {
        if (!WatchStatusSlug.TryParse(statusSlug, out var status))
            return Result<AnimeWithProgress>.Failure("Unknown watch status.", ErrorType.Validation);

        var anime = await animeRepository.GetByIdAsync(animeId, cancellationToken);
        if (anime is null)
            return Result<AnimeWithProgress>.Failure("Anime not found.", ErrorType.NotFound);

        var progress = await progressRepository.GetAsync(userId, animeId, cancellationToken);

        if (progress is null)
        {
            progress = new UserAnimeProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AnimeId = anime.Id,
                Anime = anime,
                ShikimoriId = anime.ShikimoriId ?? 0,
                Status = status
            };

            await progressRepository.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.Status = status;
            progress.Anime = anime;
            progress.UpdatedAt = DateTime.UtcNow;

            if (status == WatchingEnum.Completed && anime.Episodes is > 0)
                progress.EpisodesWatched = anime.Episodes.Value;

            await progressRepository.UpdateAsync(progress, cancellationToken);
        }

        logger.LogInformation("User {UserId} set status {Status} for anime {AnimeId}", userId, status, animeId);
        return Result<AnimeWithProgress>.Success(new AnimeWithProgress { Anime = anime, Progress = progress });
    }

    public async Task<Result> RemoveWatchStatusAsync(
        Guid userId,
        Guid animeId,
        CancellationToken cancellationToken = default)
    {
        var progress = await progressRepository.GetAsync(userId, animeId, cancellationToken);
        if (progress is not null)
        {
            await progressRepository.RemoveAsync(progress, cancellationToken);
            logger.LogInformation("User {UserId} removed progress for anime {AnimeId}", userId, animeId);
        }

        return Result.Success();
    }

    public async Task<Result> SaveProgressAsync(
        Guid userId,
        int shikimoriId,
        int episode,
        int? translationId,
        double position,
        double duration,
        CancellationToken cancellationToken = default)
    {
        if (shikimoriId <= 0)
            return Result.Failure("Invalid shikimori id.", ErrorType.Validation);

        var anime = await animeRepository.GetByShikimoriIdAsync(shikimoriId, cancellationToken);
        if (anime is null)
            return Result.Failure("Anime not found.", ErrorType.NotFound);

        var progress = await progressRepository.GetByShikimoriIdAsync(userId, shikimoriId, cancellationToken);

        if (progress is null)
        {
            progress = new UserAnimeProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AnimeId = anime.Id,
                ShikimoriId = shikimoriId,
                Status = WatchingEnum.Watching
            };

            await progressRepository.AddAsync(progress, cancellationToken);
        }

        var finishedEpisode = duration > 0 && position >= duration * 0.9;
        var watched = Math.Max(0, finishedEpisode ? episode : episode - 1);

        progress.EpisodesWatched = Math.Max(progress.EpisodesWatched, watched);
        progress.LastPlaybackPosition = Math.Max(0, position);
        progress.LastEpisodeDuration = duration > 0 ? duration : progress.LastEpisodeDuration;
        progress.LastTranslationId = translationId;
        progress.UpdatedAt = DateTime.UtcNow;

        if (progress.Status == WatchingEnum.Planned)
            progress.Status = WatchingEnum.Watching;

        await progressRepository.UpdateAsync(progress, cancellationToken);

        return Result.Success();
    }
}
