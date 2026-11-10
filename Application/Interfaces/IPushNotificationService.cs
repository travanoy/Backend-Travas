namespace Application.Interfaces;

public interface IPushNotificationService
{
    Task SendAsync(
        IEnumerable<string> expoPushTokens,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default);
}
