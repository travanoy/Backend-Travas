namespace Domain.Entity;

public class EpisodeNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AnimeId { get; set; }
    public Anime Anime { get; set; } = null!;

    public int Episode { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
