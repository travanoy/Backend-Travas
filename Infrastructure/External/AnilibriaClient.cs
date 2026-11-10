using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.External;

/// <summary>
/// Клиент публичного API AniLibria (https://anilibria.top/api/v1) — без токена.
/// Отдаёт плеер-данные в общей структуре <see cref="KodikPlayerData"/>:
/// одна "озвучка" = AniLibria, серии — HLS-ссылки.
/// </summary>
public class AnilibriaClient(
    HttpClient httpClient,
    ILogger<AnilibriaClient> logger) : IAnilibriaClient
{
    private const string TranslationTitle = "AniLibria";

    public async Task<KodikPlayerData?> GetPlayerAsync(
        int shikimoriId,
        IReadOnlyCollection<string?> titles,
        CancellationToken cancellationToken = default)
    {
        if (shikimoriId <= 0)
            return null;

        try
        {
            var releaseId = await FindReleaseIdAsync(shikimoriId, titles, cancellationToken);
            if (releaseId is null)
            {
                logger.LogInformation("Anilibria has no release for shikimori {ShikimoriId}", shikimoriId);
                return null;
            }

            var detail = await GetJsonAsync($"anime/releases/{releaseId.Value}", cancellationToken);
            if (detail is null)
                return null;

            var episodes = ParseEpisodes(detail.Value);
            if (episodes.Count == 0)
            {
                logger.LogInformation("Anilibria release {ReleaseId} has no HLS episodes", releaseId);
                return null;
            }

            return new KodikPlayerData
            {
                ShikimoriId = shikimoriId,
                Title = GetName(detail.Value) ?? string.Empty,
                Translations =
                [
                    new KodikTranslationData
                    {
                        Id = releaseId.Value,
                        Title = TranslationTitle,
                        Type = "voice",
                        Episodes = episodes
                    }
                ]
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Anilibria request failed for shikimori {ShikimoriId}", shikimoriId);
            return null;
        }
    }

    private async Task<int?> FindReleaseIdAsync(
        int shikimoriId,
        IReadOnlyCollection<string?> titles,
        CancellationToken cancellationToken)
    {
        // Фильтра по shikimori_id в API нет — ищем по названию и сверяем shikimori.id.
        foreach (var raw in titles)
        {
            var title = raw?.Trim();
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var root = await GetJsonAsync(
                $"anime/catalog/releases?f%5Bsearch%5D={Uri.EscapeDataString(title)}&limit=20",
                cancellationToken);

            if (root is null
                || !root.Value.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in data.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!item.TryGetProperty("shikimori", out var shikimori)
                    || shikimori.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (GetInt(shikimori, "id") == shikimoriId)
                    return GetInt(item, "id");
            }
        }

        return null;
    }

    private static List<KodikEpisodeData> ParseEpisodes(JsonElement detail)
    {
        var episodes = new List<KodikEpisodeData>();

        if (!detail.TryGetProperty("episodes", out var list) || list.ValueKind != JsonValueKind.Array)
            return episodes;

        foreach (var episode in list.EnumerateArray())
        {
            var number = GetInt(episode, "ordinal") ?? 0;
            if (number <= 0)
                continue;

            var url = GetString(episode, "hls_720")
                ?? GetString(episode, "hls_1080")
                ?? GetString(episode, "hls_480");

            if (string.IsNullOrWhiteSpace(url))
                continue;

            var name = GetString(episode, "name");

            episodes.Add(new KodikEpisodeData
            {
                Number = number,
                Title = string.IsNullOrWhiteSpace(name) ? $"Episode {number}" : name,
                Url = url
            });
        }

        return episodes
            .OrderBy(episode => episode.Number)
            .ToList();
    }

    private async Task<JsonElement?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Anilibria returned {Status} for {Url}", response.StatusCode, url);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static string? GetName(JsonElement detail)
    {
        if (!detail.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.Object)
            return null;

        return GetString(name, "english") ?? GetString(name, "main");
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
