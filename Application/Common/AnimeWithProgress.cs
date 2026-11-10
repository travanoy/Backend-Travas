using Domain.Entity;

namespace Application.Common;

public class AnimeWithProgress
{
    public required Anime Anime { get; init; }
    public UserAnimeProgress? Progress { get; init; }
}

public class ContinueWatchingItem
{
    public required Anime Anime { get; init; }
    public required UserAnimeProgress Progress { get; init; }
}
