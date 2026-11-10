using Application.Common;
using Domain.Entity;

namespace Application.Services.Abstract;

public interface IAnimeService
{
    Task<Result<PagedResult<AnimeWithProgress>>> SearchAsync(
        string? search,
        string? genre,
        string? status,
        int page,
        int pageSize,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<Result<AnimeWithProgress>> GetByIdAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task<Result<AnimeWithProgress>> GetByShikimoriIdAsync(int shikimoriId, Guid? userId, CancellationToken cancellationToken = default);
    Task<Result<Anime>> GetBannerAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<string>>> GetGenresAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetStatuses();
    Task<Result<IReadOnlyList<ContinueWatchingItem>>> GetContinueWatchingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AnimeWithProgress>>> GetNewEpisodesAsync(Guid? userId, int limit = 12, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AnimeWithProgress>>> GetSavedAsync(Guid userId, string? statusSlug, int page, int pageSize, CancellationToken cancellationToken = default);
}
