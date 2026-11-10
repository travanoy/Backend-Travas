using Domain.Entity;

namespace Application.Interfaces;

public interface INotificationRepository
{
    Task AddRangeAsync(IEnumerable<EpisodeNotification> notifications, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid animeId, int episode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EpisodeNotification>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
}
