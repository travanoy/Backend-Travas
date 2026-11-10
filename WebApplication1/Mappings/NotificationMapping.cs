using Domain.Entity;
using WebApplication1.DTOs;

namespace WebApplication1.Mappings;

public static class NotificationMapping
{
    public static NotificationDto ToDto(this EpisodeNotification notification) => new()
    {
        Id = notification.Id,
        AnimeId = notification.AnimeId,
        AnimeTitle = notification.Anime?.Title ?? string.Empty,
        CoverImage = notification.Anime?.ImageUrl ?? string.Empty,
        Episode = notification.Episode,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
