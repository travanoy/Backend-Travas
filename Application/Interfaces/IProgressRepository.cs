using Domain.Entity;

namespace Application.Interfaces;

public interface IProgressRepository
{
    Task<UserAnimeProgress?> GetAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default);
    Task<UserAnimeProgress?> GetByShikimoriIdAsync(Guid userId, int shikimoriId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAnimeProgress>> GetByUserAsync(Guid userId, WatchingEnum? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAnimeProgress>> GetContinueWatchingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAnimeProgress>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetPushSubscribersAsync(int shikimoriId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetTrackedShikimoriIdsAsync(
        IReadOnlyCollection<WatchingEnum> statuses,
        CancellationToken cancellationToken = default);
}
