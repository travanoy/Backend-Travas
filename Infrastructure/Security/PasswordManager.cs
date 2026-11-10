using Application.Interfaces;
using Domain.Entity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public class PasswordManager : IPasswordManager
{
    private readonly PasswordHasher<User> hasher = new();

    // PasswordHasher не использует сам объект пользователя при PBKDF2, поэтому хватает заглушки.
    private static readonly User Placeholder = new()
    {
        Username = "placeholder",
        Email = "placeholder@travas.local",
        PasswordHash = string.Empty
    };

    public string Hash(string password) => hasher.HashPassword(Placeholder, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = hasher.VerifyHashedPassword(Placeholder, passwordHash, password);
        return result != PasswordVerificationResult.Failed;
    }
}
