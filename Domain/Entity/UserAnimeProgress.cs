namespace Domain.Entity;

public class UserAnimeProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AnimeId { get; set; }
    public Anime Anime { get; set; } = null!;

    public int ShikimoriId { get; set; }

    public WatchingEnum Status { get; set; } = WatchingEnum.Planned;
    public int EpisodesWatched { get; set; }
    public int? UserScore { get; set; }
    public bool IsFavorite { get; set; }

    public int? LastTranslationId { get; set; }
    public double LastPlaybackPosition { get; set; }
    public double LastEpisodeDuration { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
