using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;

namespace Application.Services.Concrete;

public class ReviewService(IReviewRepository reviewRepository, IAnimeRepository animeRepository) : IReviewService
{
    private const int MinRating = 1;
    private const int MaxRating = 5;
    private const int MaxTextLength = 2000;

    public async Task<Result<IReadOnlyList<Review>>> GetForAnimeAsync(Guid animeId, CancellationToken cancellationToken = default)
    {
        var reviews = await reviewRepository.GetByAnimeAsync(animeId, cancellationToken);
        return Result<IReadOnlyList<Review>>.Success(reviews);
    }

    public async Task<Result<Review>> UpsertAsync(
        Guid userId,
        Guid animeId,
        int rating,
        string? text,
        CancellationToken cancellationToken = default)
    {
        if (rating < MinRating || rating > MaxRating)
            return Result<Review>.Failure($"Rating must be between {MinRating} and {MaxRating}.", ErrorType.Validation);

        var normalizedText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (normalizedText is { Length: > MaxTextLength })
            return Result<Review>.Failure($"Review text must be at most {MaxTextLength} characters.", ErrorType.Validation);

        var anime = await animeRepository.GetByIdAsync(animeId, cancellationToken);
        if (anime is null)
            return Result<Review>.Failure("Anime not found.", ErrorType.NotFound);

        var existing = await reviewRepository.GetByUserAsync(userId, animeId, cancellationToken);
        if (existing is null)
        {
            var review = new Review
            {
                UserId = userId,
                AnimeId = animeId,
                Rating = rating,
                Text = normalizedText
            };

            await reviewRepository.AddAsync(review, cancellationToken);
            return Result<Review>.Success(review);
        }

        existing.Rating = rating;
        existing.Text = normalizedText;
        existing.UpdatedAt = DateTime.UtcNow;
        await reviewRepository.UpdateAsync(existing, cancellationToken);

        return Result<Review>.Success(existing);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default)
    {
        var existing = await reviewRepository.GetByUserAsync(userId, animeId, cancellationToken);
        if (existing is null)
            return Result.Failure("Review not found.", ErrorType.NotFound);

        await reviewRepository.DeleteAsync(existing, cancellationToken);
        return Result.Success();
    }
}
