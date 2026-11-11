using Application.Interfaces;
using Domain.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public async Task<IReadOnlyList<Review>> GetByAnimeAsync(Guid animeId, CancellationToken cancellationToken = default)
        => await context.Reviews
            .AsNoTracking()
            .Include(review => review.User)
            .Where(review => review.AnimeId == animeId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Review?> GetByUserAsync(Guid userId, Guid animeId, CancellationToken cancellationToken = default)
        => await context.Reviews
            .Include(review => review.User)
            .FirstOrDefaultAsync(review => review.UserId == userId && review.AnimeId == animeId, cancellationToken);

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        if (review.Id == Guid.Empty)
            review.Id = Guid.NewGuid();

        await context.Reviews.AddAsync(review, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Review review, CancellationToken cancellationToken = default)
    {
        context.Reviews.Update(review);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Review review, CancellationToken cancellationToken = default)
    {
        context.Reviews.Remove(review);
        await context.SaveChangesAsync(cancellationToken);
    }
}
