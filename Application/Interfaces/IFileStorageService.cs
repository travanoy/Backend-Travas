namespace Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAvatarAsync(Stream stream, string originalFileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default);
}
