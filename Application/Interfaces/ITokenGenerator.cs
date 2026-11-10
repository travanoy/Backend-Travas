using Application.Common;
using Domain.Entity;

namespace Application.Interfaces;

public interface ITokenGenerator
{
    string GenerateAccessToken(User user);
    RefreshTokenResult GenerateRefreshToken();
    string HashRefreshToken(string token);
}
