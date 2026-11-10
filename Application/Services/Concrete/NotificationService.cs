using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;

namespace Application.Services.Concrete;

public class NotificationService(INotificationRepository notificationRepository) : INotificationService
{
    public async Task<PagedResult<EpisodeNotification>> GetAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await notificationRepository.CountAsync(userId, cancellationToken);
        var items = await notificationRepository.GetByUserAsync(userId, page, pageSize, cancellationToken);

        return new PagedResult<EpisodeNotification>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => notificationRepository.CountUnreadAsync(userId, cancellationToken);

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => notificationRepository.MarkAllReadAsync(userId, cancellationToken);

    public Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        => notificationRepository.MarkReadAsync(userId, notificationId, cancellationToken);
}
