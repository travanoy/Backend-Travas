using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.External;

public class ShikimoriClient(
    HttpClient httpClient,
    ILogger<ShikimoriClient> logger) : IShikimoriClient
{
    private readonly int pageSize = 50;

    private const string Query = """
        query Animes($ids: String, $search: String, $limit: Int, $page: Int) {
          animes(ids: $ids, search: $search, limit: $limit, page: $page) {
            id
            name
            russian
            english
            japanese
            synonyms
            kind
            status
            score
            episodes
            episodesAired
            nextEpisodeAt
            season
            description
            airedOn { year }
            genres { name russian }
            studios { name }
            poster { originalUrl mainUrl }
          }
        }
        """;

    private const string OngoingQuery = """
        query Ongoing($limit: Int, $page: Int) {
          animes(status: "ongoing", limit: $limit, page: $page) {
            id
            name
            russian
            english
            japanese
            synonyms
            kind
            status
            score
            episodes
            episodesAired
            nextEpisodeAt
            season
            description
            airedOn { year }
            genres { name russian }
            studios { name }
            poster { originalUrl mainUrl }
          }
        }
        """;

    public Task<IReadOnlyList<ShikimoriAnimeData>> SearchAsync(string search, CancellationToken cancellationToken = default)
        => ExecuteAsync(Query, new Dictionary<string, object?>
        {
            ["search"] = search,
            ["limit"] = pageSize
        }, cancellationToken);

    public async Task<IReadOnlyList<ShikimoriAnimeData>> GetPopularAsync(int limit, CancellationToken cancellationToken = default)
    {
        var collected = new List<ShikimoriAnimeData>();
        var page = 1;

        while (collected.Count < limit)
        {
            var batch = await ExecuteAsync(Query, new Dictionary<string, object?>
            {
                ["limit"] = pageSize,
                ["page"] = page
            }, cancellationToken);

            if (batch.Count == 0)
                break;

            collected.AddRange(batch);
            page++;

            if (batch.Count < pageSize)
                break;

            await Task.Delay(250, cancellationToken);
        }

        return collected
            .GroupBy(anime => anime.Id)
            .Select(group => group.First())
            .OrderByDescending(anime => anime.Score ?? 0)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<ShikimoriAnimeData>> GetOngoingAsync(int limit, CancellationToken cancellationToken = default)
    {
        var collected = new List<ShikimoriAnimeData>();
        var page = 1;

        while (collected.Count < limit)
        {
            var batch = await ExecuteAsync(OngoingQuery, new Dictionary<string, object?>
            {
                ["limit"] = pageSize,
                ["page"] = page
            }, cancellationToken);

            if (batch.Count == 0)
                break;

            collected.AddRange(batch);
            page++;

            if (batch.Count < pageSize)
                break;

            await Task.Delay(250, cancellationToken);
        }

        return collected
            .GroupBy(anime => anime.Id)
            .Select(group => group.First())
            .Take(limit)
            .ToList();
    }

    public async Task<ShikimoriAnimeData?> GetByIdAsync(int shikimoriId, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(Query, new Dictionary<string, object?>
        {
            ["ids"] = shikimoriId.ToString(),
            ["limit"] = 1
        }, cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ShikimoriAnimeData>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        var collected = new List<ShikimoriAnimeData>();

        foreach (var chunk in ids.Distinct().Chunk(50))
        {
            var batch = await ExecuteAsync(Query, new Dictionary<string, object?>
            {
                ["ids"] = string.Join(",", chunk),
                ["limit"] = chunk.Length
            }, cancellationToken);

            collected.AddRange(batch);
            await Task.Delay(250, cancellationToken);
        }

        return collected;
    }

    private async Task<IReadOnlyList<ShikimoriAnimeData>> ExecuteAsync(
        string query,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/graphql", new
            {
                query,
                variables
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Shikimori returned {Status}", response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors))
            {
                logger.LogWarning("Shikimori GraphQL errors: {Errors}", errors.ToString());
                return [];
            }

            if (!root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("animes", out var animes)
                || animes.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var target = new List<ShikimoriAnimeData>();

            foreach (var element in animes.EnumerateArray())
                target.Add(MapAnime(element));

            return target;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Shikimori request failed");
            return [];
        }
    }

    private static ShikimoriAnimeData MapAnime(JsonElement element) => new()
    {
        Id = GetInt(element, "id") ?? 0,
        Name = GetString(element, "name"),
        Russian = GetString(element, "russian"),
        English = GetString(element, "english"),
        Japanese = GetString(element, "japanese"),
        Synonyms = GetStringList(element, "synonyms"),
        Kind = GetString(element, "kind"),
        Status = GetString(element, "status"),
        Score = GetDouble(element, "score"),
        Episodes = GetInt(element, "episodes"),
        EpisodesAired = GetInt(element, "episodesAired"),
        NextEpisodeAt = GetDateTime(element, "nextEpisodeAt"),
        Season = GetString(element, "season"),
        Description = GetString(element, "description"),
        Year = element.TryGetProperty("airedOn", out var airedOn) && airedOn.ValueKind == JsonValueKind.Object
            ? GetInt(airedOn, "year")
            : null,
        PosterUrl = element.TryGetProperty("poster", out var poster) && poster.ValueKind == JsonValueKind.Object
            ? GetString(poster, "originalUrl") ?? GetString(poster, "mainUrl")
            : null,
        Genres = GetNamedList(element, "genres"),
        Studios = GetNamedList(element, "studios")
    };

    private static string? GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt32(),
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static double? GetDouble(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static DateTime? GetDateTime(JsonElement element, string property)
        => element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.String
           && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;

    private static List<string> GetStringList(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static List<string> GetNamedList(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];

        return value.EnumerateArray()
            .Select(item => GetString(item, "russian") ?? GetString(item, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();
    }
}
