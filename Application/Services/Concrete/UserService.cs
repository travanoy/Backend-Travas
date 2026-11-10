using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class UserService(
    IUserRepository userRepository,
    IProgressRepository progressRepository,
    IFileStorageService fileStorage,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<User>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? Result<User>.Failure("User not found.", ErrorType.NotFound)
            : Result<User>.Success(user);
    }

    public async Task<Result<User>> UpdateNameAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<User>.Failure("User not found.", ErrorType.NotFound);

        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length < 2)
            return Result<User>.Failure("Name must be at least 2 characters long.", ErrorType.Validation);

        if (normalized.Length > 50)
            return Result<User>.Failure("Name must be 50 characters or less.", ErrorType.Validation);

        if (!string.Equals(user.Username, normalized, StringComparison.OrdinalIgnoreCase)
            && await userRepository.ExistsByUsernameAsync(normalized, cancellationToken))
        {
            return Result<User>.Failure("This nickname is already taken.", ErrorType.Conflict);
        }

        user.Username = normalized;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("User {Id} changed name to {Name}", user.Id, normalized);
        return Result<User>.Success(user);
    }

    public async Task<Result<User>> UpdateAvatarAsync(
        Guid userId,
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<User>.Failure("User not found.", ErrorType.NotFound);

        var previousAvatar = user.AvatarUrl;
        user.AvatarUrl = await fileStorage.SaveAvatarAsync(stream, fileName, cancellationToken);
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousAvatar))
            await fileStorage.DeleteAsync(previousAvatar, cancellationToken);

        logger.LogInformation("User {Id} updated avatar", user.Id);
        return Result<User>.Success(user);
    }

    public async Task<Result> SetPushTokenAsync(
        Guid userId,
        string? expoPushToken,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.", ErrorType.NotFound);

        user.ExpoPushToken = string.IsNullOrWhiteSpace(expoPushToken) ? null : expoPushToken.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserStats>> GetStatsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var progresses = await progressRepository.GetAllByUserAsync(userId, cancellationToken);

        var episodesWatched = progresses.Sum(progress => progress.EpisodesWatched);

        var stats = new UserStats
        {
            Watching = progresses.Count(progress => progress.Status == WatchingEnum.Watching),
            Completed = progresses.Count(progress => progress.Status == WatchingEnum.Completed),
            Planned = progresses.Count(progress => progress.Status == WatchingEnum.Planned),
            OnHold = progresses.Count(progress => progress.Status == WatchingEnum.OnHold),
            Dropped = progresses.Count(progress => progress.Status == WatchingEnum.Dropped),
            TotalAnime = progresses.Count,
            EpisodesWatched = episodesWatched,
            // Средняя длительность серии ~24 минуты.
            DaysWatched = episodesWatched * 24.0 / 60.0 / 24.0
        };

        return Result<UserStats>.Success(stats);
    }
}
