using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Infrastructure.External;

public class ExpoPushService(HttpClient httpClient, ILogger<ExpoPushService> logger) : IPushNotificationService
{
    private const string Endpoint = "https://exp.host/--/api/v2/push/send";

    public async Task SendAsync(
        IEnumerable<string> expoPushTokens,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = expoPushTokens
            .Where(IsExpoToken)
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
            return;

        foreach (var chunk in tokens.Chunk(100))
        {
            var messages = chunk.Select(token => new
            {
                to = token,
                title,
                body,
                sound = "default",
                data
            }).ToList();

            try
            {
                using var response = await httpClient.PostAsJsonAsync(Endpoint, messages, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Expo push returned {Status}", response.StatusCode);
                    continue;
                }

                logger.LogInformation("Expo push sent to {Count} devices", chunk.Length);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expo push failed");
            }
        }
    }

    private static bool IsExpoToken(string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && (token.StartsWith("ExponentPushToken", StringComparison.Ordinal)
            || token.StartsWith("ExpoPushToken", StringComparison.Ordinal));
}
