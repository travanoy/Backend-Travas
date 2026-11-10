using Application.Common;

namespace Application.Services.Abstract;

public interface IAuthService
{
    Task<Result<AuthResult>> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string? refreshToken, Guid? callerId = null, CancellationToken cancellationToken = default);
}
