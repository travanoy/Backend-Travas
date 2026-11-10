using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordManager passwordManager,
    ITokenGenerator tokenGenerator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<AuthResult>> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            return Result<AuthResult>.Failure("Name must be at least 2 characters long.", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<AuthResult>.Failure("A valid email is required.", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return Result<AuthResult>.Failure("Password must be at least 6 characters long.", ErrorType.Validation);

        var normalizedName = name.Trim();
        var normalizedEmail = NormalizeEmail(email);

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            logger.LogWarning("Registration rejected: email {Email} already exists", normalizedEmail);
            return Result<AuthResult>.Failure("An account with this email already exists.", ErrorType.Conflict);
        }

        if (await userRepository.ExistsByUsernameAsync(normalizedName, cancellationToken))
        {
            logger.LogWarning("Registration rejected: username {Name} already exists", normalizedName);
            return Result<AuthResult>.Failure("This nickname is already taken.", ErrorType.Conflict);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedName,
            Email = normalizedEmail,
            PasswordHash = passwordManager.Hash(password)
        };

        await userRepository.AddAsync(user, cancellationToken);
        logger.LogInformation("User {Id} ({Email}) registered", user.Id, normalizedEmail);

        return Result<AuthResult>.Success(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResult>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !passwordManager.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Login failed for email {Email}", email);
            return Result<AuthResult>.Failure("Invalid email or password.", ErrorType.Unauthorized);
        }

        logger.LogInformation("User {Id} logged in", user.Id);
        return Result<AuthResult>.Success(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<AuthResult>.Failure("Refresh token is required.", ErrorType.Unauthorized);

        var hash = tokenGenerator.HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (stored is null)
        {
            logger.LogWarning("Refresh failed: token not found");
            return Result<AuthResult>.Failure("Invalid refresh token.", ErrorType.Unauthorized);
        }

        // Ротация: старый refresh удаляем всегда, даже если он уже истёк.
        await refreshTokenRepository.RemoveAsync(stored, cancellationToken);

        if (stored.ExpiresAt <= DateTime.UtcNow)
        {
            logger.LogWarning("Refresh failed: token expired for user {Id}", stored.UserId);
            return Result<AuthResult>.Failure("Refresh token expired.", ErrorType.Unauthorized);
        }

        logger.LogInformation("Tokens refreshed for user {Id}", stored.UserId);
        return Result<AuthResult>.Success(await IssueTokensAsync(stored.User, cancellationToken));
    }

    public async Task<Result> LogoutAsync(
        string? refreshToken,
        Guid? callerId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        var hash = tokenGenerator.HashRefreshToken(refreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (stored is not null && (callerId is null || stored.UserId == callerId))
        {
            await refreshTokenRepository.RemoveAsync(stored, cancellationToken);
            logger.LogInformation("User {Id} logged out", stored.UserId);
        }

        return Result.Success();
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = tokenGenerator.GenerateAccessToken(user);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            TokenHash = tokenGenerator.HashRefreshToken(refreshToken.Token),
            ExpiresAt = refreshToken.ExpiresAt,
            UserId = user.Id
        }, cancellationToken);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = user
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
