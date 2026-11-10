using Application.Interfaces;
using Domain.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProgressRepository(AppDbContext context) : IProgressRepository
{
    public async Task<UserAnimeProgress?> GetAsync(
        Guid userId,
        Guid animeId,
        CancellationToken cancellationToken = default)
        => await context.UserAnimeProgresses
            .FirstOrDefaultAsync(progress => progress.UserId == userId && progress.AnimeId == animeId, cancellationToken);

    public async Task<UserAnimeProgress?> GetByShikimoriIdAsync(
        Guid userId,
        int shikimoriId,
        CancellationToken cancellationToken = default)
        => await context.UserAnimeProgresses
            .FirstOrDefaultAsync(progress => progress.UserId == userId && progress.ShikimoriId == shikimoriId, cancellationToken);

    public async Task<IReadOnlyList<UserAnimeProgress>> GetByUserAsync(
        Guid userId,
        WatchingEnum? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.UserAnimeProgresses
            .AsNoTracking()
            .Include(progress => progress.Anime)
            .Where(progress => progress.UserId == userId);

        if (status is not null)
            query = query.Where(progress => progress.Status == status);

        return await query
            .OrderByDescending(progress => progress.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserAnimeProgress>> GetContinueWatchingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await context.UserAnimeProgresses
            .AsNoTracking()
            .Include(progress => progress.Anime)
            .Where(progress => progress.UserId == userId
                && progress.Status == WatchingEnum.Watching
                && progress.LastPlaybackPosition > 0)
            .OrderByDescending(progress => progress.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAnimeProgress>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await context.UserAnimeProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default)
    {
        await context.UserAnimeProgresses.AddAsync(progress, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default)
    {
        context.UserAnimeProgresses.Update(progress);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(UserAnimeProgress progress, CancellationToken cancellationToken = default)
    {
        context.UserAnimeProgresses.Remove(progress);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetPushSubscribersAsync(
        int shikimoriId,
        CancellationToken cancellationToken = default)
    {
        var users = await context.UserAnimeProgresses
            .AsNoTracking()
            .Include(progress => progress.User)
            .Where(progress => progress.ShikimoriId == shikimoriId
                && (progress.Status == WatchingEnum.Watching || progress.Status == WatchingEnum.Planned))
            .Select(progress => progress.User)
            .Distinct()
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<IReadOnlyList<int>> GetTrackedShikimoriIdsAsync(
        IReadOnlyCollection<WatchingEnum> statuses,
        CancellationToken cancellationToken = default)
    {
        var statusList = statuses.ToList();
        if (statusList.Count == 0)
            return [];

        return await context.UserAnimeProgresses
            .AsNoTracking()
            .Where(progress => progress.ShikimoriId > 0 && statusList.Contains(progress.Status))
            .Select(progress => progress.ShikimoriId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
