namespace WebApplication1.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid AnimeId { get; set; }
    public string AnimeTitle { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public int Episode { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
