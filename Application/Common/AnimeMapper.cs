using System.Net;
using System.Text.RegularExpressions;
using Domain.Entity;

namespace Application.Common;

public static partial class AnimeMapper
{
    private const string ShikimoriHost = "https://shikimori.io";

    public static Anime FromShikimori(ShikimoriAnimeData data)
    {
        var existingBy = DateTime.UtcNow;

        return new Anime
        {
            Id = Guid.NewGuid(),
            ShikimoriId = data.Id,
            Title = FirstNotEmpty(data.Russian, data.Name, data.English, $"Anime #{data.Id}")!,
            TitleRussian = data.Russian,
            TitleEnglish = data.English,
            TitleJapanese = data.Japanese,
            TitleSynonyms = data.Synonyms,
            ImageUrl = NormalizeUrl(data.PosterUrl),
            Type = MapType(data.Kind),
            Episodes = data.Episodes,
            EpisodesAired = data.EpisodesAired ?? 0,
            NextEpisodeAt = data.NextEpisodeAt,
            Status = MapStatus(data.Status),
            Score = data.Score,
            Year = data.Year,
            Season = data.Season,
            Synopsis = CleanDescription(data.Description),
            Genres = data.Genres,
            Studios = data.Studios,
            CreatedAt = existingBy,
            UpdatedAt = existingBy
        };
    }

    public static void ApplyShikimori(Anime anime, ShikimoriAnimeData data)
    {
        anime.Title = FirstNotEmpty(data.Russian, data.Name, data.English, anime.Title)!;
        anime.TitleRussian = data.Russian ?? anime.TitleRussian;
        anime.TitleEnglish = data.English ?? anime.TitleEnglish;
        anime.TitleJapanese = data.Japanese ?? anime.TitleJapanese;
        anime.TitleSynonyms = data.Synonyms.Count > 0 ? data.Synonyms : anime.TitleSynonyms;
        anime.ImageUrl = NormalizeUrl(data.PosterUrl) ?? anime.ImageUrl;
        anime.Type = MapType(data.Kind);
        anime.Episodes = data.Episodes ?? anime.Episodes;
        anime.EpisodesAired = data.EpisodesAired ?? anime.EpisodesAired;
        anime.NextEpisodeAt = data.NextEpisodeAt ?? anime.NextEpisodeAt;
        anime.Status = MapStatus(data.Status);
        anime.Score = data.Score ?? anime.Score;
        anime.Year = data.Year ?? anime.Year;
        anime.Season = data.Season ?? anime.Season;
        anime.Synopsis = CleanDescription(data.Description) ?? anime.Synopsis;
        anime.Genres = data.Genres.Count > 0 ? data.Genres : anime.Genres;
        anime.Studios = data.Studios.Count > 0 ? data.Studios : anime.Studios;
        anime.UpdatedAt = DateTime.UtcNow;
    }

    private static AnimeType MapType(string? kind) => kind?.ToLowerInvariant() switch
    {
        "movie" => AnimeType.Movie,
        "ova" => AnimeType.OVA,
        "ona" => AnimeType.ONA,
        "special" or "tv_special" => AnimeType.Special,
        "music" => AnimeType.Music,
        _ => AnimeType.TV
    };

    private static AnimeStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "ongoing" => AnimeStatus.CurrentlyAiring,
        "anons" => AnimeStatus.NotYetAired,
        _ => AnimeStatus.FinishedAiring
    };

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith("//"))
            return $"https:{url}";

        if (url.StartsWith('/'))
            return $"{ShikimoriHost}{url}";

        return url;
    }

    private static string? CleanDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var withoutTags = HtmlTagRegex().Replace(description, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();
}
