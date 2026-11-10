namespace Application.Interfaces;

public interface IPasswordManager
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
