using Application.Interfaces;
using Domain.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task AddRangeAsync(
        IEnumerable<EpisodeNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        var items = notifications.ToList();
        if (items.Count == 0)
            return;

        await context.EpisodeNotifications.AddRangeAsync(items, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid animeId,
        int episode,
        CancellationToken cancellationToken = default)
        => await context.EpisodeNotifications
            .AsNoTracking()
            .AnyAsync(notification => notification.UserId == userId
                && notification.AnimeId == animeId
                && notification.Episode == episode, cancellationToken);

    public async Task<IReadOnlyList<EpisodeNotification>> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await context.EpisodeNotifications
            .AsNoTracking()
            .Include(notification => notification.Anime)
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.EpisodeNotifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId, cancellationToken);

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.EpisodeNotifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.EpisodeNotifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(setter => setter.SetProperty(notification => notification.IsRead, true), cancellationToken);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var updated = await context.EpisodeNotifications
            .Where(notification => notification.UserId == userId && notification.Id == notificationId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(notification => notification.IsRead, true), cancellationToken);

        return updated > 0;
    }
}
