using Application.Common;
using Domain.Entity;

namespace Application.Services.Abstract;

public interface IUserService
{
    Task<Result<User>> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<User>> UpdateNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);
    Task<Result<User>> UpdateAvatarAsync(Guid userId, Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Result> SetPushTokenAsync(Guid userId, string? expoPushToken, CancellationToken cancellationToken = default);
    Task<Result<UserStats>> GetStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}
