namespace Domain.Entity;

public class RefreshToken
{
    public int Id { get; set; }

    // В БД лежит только SHA-256 хеш, сырой токен клиенту выдаётся один раз.
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
