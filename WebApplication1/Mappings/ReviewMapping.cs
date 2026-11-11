using Domain.Entity;
using WebApplication1.DTOs;

namespace WebApplication1.Mappings;

public static class ReviewMapping
{
    public static ReviewDto ToDto(this Review review) => new()
    {
        Id = review.Id,
        AnimeId = review.AnimeId,
        Rating = review.Rating,
        Text = review.Text ?? string.Empty,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt,
        AuthorId = review.UserId,
        AuthorName = review.User?.Username ?? string.Empty,
        AuthorAvatar = review.User?.AvatarUrl
    };
}
