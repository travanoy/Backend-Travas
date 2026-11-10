using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class LocalFileStorageService(
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public async Task<string> SaveAvatarAsync(
        Stream stream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Only jpg, jpeg, png and webp images are allowed.", nameof(originalFileName));

        var folder = Path.Combine(GetWebRoot(), "uploads", "avatars");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(folder, fileName);

        await using (var file = File.Create(path))
            await stream.CopyToAsync(file, cancellationToken);

        var request = httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is null ? string.Empty : $"{request.Scheme}://{request.Host}";

        logger.LogInformation("Avatar saved as {FileName}", fileName);
        return $"{baseUrl}/uploads/avatars/{fileName}";
    }

    public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicUrl) || !publicUrl.Contains("/uploads/avatars/"))
            return Task.CompletedTask;

        var fileName = Path.GetFileName(publicUrl);
        var path = Path.Combine(GetWebRoot(), "uploads", "avatars", fileName);

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to delete avatar {File}", fileName);
        }

        return Task.CompletedTask;
    }

    private string GetWebRoot() =>
        string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
}
