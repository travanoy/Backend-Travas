namespace WebApplication1.DTOs;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid AnimeId { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
}

public class UpsertReviewDto
{
    public int Rating { get; set; }
    public string? Text { get; set; }
}
