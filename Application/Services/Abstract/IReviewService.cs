using Application.Common;
using Domain.Entity;

namespace Application.Services.Abstract;

public interface IReviewService
{
    Task<Result<IReadOnlyList<Review>>> GetForAnimeAsync(Guid animeId, CancellationToken cancellationToken = default);
    Task<Result<Review>> UpsertAsync(Guid userId, Guid animeId, int rating, string? text, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default);
}
