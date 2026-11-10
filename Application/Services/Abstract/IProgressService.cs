using Application.Common;
using Domain.Entity;

namespace Application.Services.Abstract;

public interface IProgressService
{
    Task<Result<AnimeWithProgress>> SetWatchStatusAsync(
        Guid userId,
        Guid animeId,
        string statusSlug,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveWatchStatusAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default);

    Task<Result> SaveProgressAsync(
        Guid userId,
        int shikimoriId,
        int episode,
        int? translationId,
        double position,
        double duration,
        CancellationToken cancellationToken = default);
}
