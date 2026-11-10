using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.External;

public class KodikClient(
    HttpClient httpClient,
    IOptions<KodikOptions> options,
    ILogger<KodikClient> logger) : IKodikClient
{
    private readonly KodikOptions settings = options.Value;

    public async Task<KodikPlayerData?> GetPlayerAsync(int shikimoriId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.Token))
        {
            logger.LogWarning("Kodik token is not configured; player sources are unavailable");
            return null;
        }

        try
        {
            var url = $"search?token={Uri.EscapeDataString(settings.Token)}&shikimori_id={shikimoriId}&with_material_data=true&limit=100";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Kodik returned {Status} for anime {ShikimoriId}", response.StatusCode, shikimoriId);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array)
            {
                return new KodikPlayerData { ShikimoriId = shikimoriId };
            }

            var translations = new Dictionary<int, KodikTranslationData>();

            foreach (var item in results.EnumerateArray())
            {
                var link = GetString(item, "link");
                if (string.IsNullOrWhiteSpace(link))
                    continue;

                var translationElement = item.TryGetProperty("translation", out var value) && value.ValueKind == JsonValueKind.Object
                    ? value
                    : default;

                var translationId = GetInt(translationElement, "id") ?? 0;
                var translationTitle = GetString(translationElement, "title") ?? "Unknown";
                var translationType = GetString(translationElement, "type") ?? "voice";

                if (translations.ContainsKey(translationId))
                    continue;

                var episodesCount = GetInt(item, "episodes_count")
                    ?? GetInt(item, "last_episode")
                    ?? 1;

                if (episodesCount < 1)
                    episodesCount = 1;

                var episodes = new List<KodikEpisodeData>(episodesCount);
                for (var number = 1; number <= episodesCount; number++)
                {
                    episodes.Add(new KodikEpisodeData
                    {
                        Number = number,
                        Title = $"Episode {number}",
                        Url = BuildEpisodeUrl(link, number)
                    });
                }

                translations[translationId] = new KodikTranslationData
                {
                    Id = translationId,
                    Title = translationTitle,
                    Type = translationType,
                    Episodes = episodes
                };
            }

            return new KodikPlayerData
            {
                ShikimoriId = shikimoriId,
                Title = GetMaterialTitle(results) ?? string.Empty,
                Translations = translations.Values.ToList()
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Kodik request failed for anime {ShikimoriId}", shikimoriId);
            return null;
        }
    }

    public async Task<IReadOnlyList<KodikUpdatedItem>> GetRecentlyUpdatedAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.Token))
        {
            logger.LogWarning("Kodik token is not configured; episode check skipped");
            return [];
        }

        try
        {
            var url = $"list?token={Uri.EscapeDataString(settings.Token)}&sort=updated_at&order=desc&limit={limit}";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Kodik list returned {Status}", response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var target = new List<KodikUpdatedItem>();

            foreach (var item in results.EnumerateArray())
            {
                var shikimoriId = GetInt(item, "shikimori_id");
                if (shikimoriId is null or <= 0)
                    continue;

                target.Add(new KodikUpdatedItem
                {
                    ShikimoriId = shikimoriId.Value,
                    LastEpisode = GetInt(item, "last_episode")
                });
            }

            return target;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Kodik updated list request failed");
            return [];
        }
    }

    private static string BuildEpisodeUrl(string link, int episode)
    {
        var normalized = link.StartsWith("//") ? $"https:{link}" : link;

        if (normalized.Contains("episode=", StringComparison.OrdinalIgnoreCase))
            return normalized;

        var separator = normalized.Contains('?') ? '&' : '?';
        return $"{normalized}{separator}episode={episode}";
    }

    private static string? GetMaterialTitle(JsonElement results)
    {
        foreach (var item in results.EnumerateArray())
        {
            if (!item.TryGetProperty("material_data", out var material) || material.ValueKind != JsonValueKind.Object)
                continue;

            var title = GetString(material, "anime_title") ?? GetString(material, "title");
            if (!string.IsNullOrWhiteSpace(title))
                return title;
        }

        return null;
    }

    private static string? GetString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt32(),
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
