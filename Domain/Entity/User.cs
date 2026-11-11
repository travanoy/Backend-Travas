namespace Domain.Entity;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public string? AvatarUrl { get; set; }

    
    public string? ExpoPushToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserAnimeProgress> AnimeProgresses { get; set; } = new List<UserAnimeProgress>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
