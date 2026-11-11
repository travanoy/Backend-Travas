using Domain.Entity;

namespace Application.Interfaces;

public interface IReviewRepository
{
    Task<IReadOnlyList<Review>> GetByAnimeAsync(Guid animeId, CancellationToken cancellationToken = default);
    Task<Review?> GetByUserAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
    Task UpdateAsync(Review review, CancellationToken cancellationToken = default);
    Task DeleteAsync(Review review, CancellationToken cancellationToken = default);
}
