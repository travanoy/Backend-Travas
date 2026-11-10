using Application.Common;
using Domain.Entity;
using WebApplication1.DTOs;

namespace WebApplication1.Mappings;

public static class UserMapping
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Name = user.Username,
        Email = user.Email,
        Avatar = user.AvatarUrl,
        CreatedAt = user.CreatedAt
    };

    public static UserStatsDto ToDto(this UserStats stats) => new()
    {
        Watching = stats.Watching,
        Completed = stats.Completed,
        Planned = stats.Planned,
        OnHold = stats.OnHold,
        Dropped = stats.Dropped,
        TotalAnime = stats.TotalAnime,
        EpisodesWatched = stats.EpisodesWatched,
        DaysWatched = stats.DaysWatched
    };
}
