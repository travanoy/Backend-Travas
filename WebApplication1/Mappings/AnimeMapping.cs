using Application.Common;
using Domain.Entity;
using WebApplication1.DTOs;

namespace WebApplication1.Mappings;

public static class AnimeMapping
{
    private const double DefaultEpisodeSeconds = 24 * 60;

    public static AnimeDetailDto ToDetail(this AnimeWithProgress source)
    {
        var anime = source.Anime;
        var progress = source.Progress;
        var playback = Playback(anime, progress);

        return new AnimeDetailDto
        {
            Id = anime.Id,
            ShikimoriId = anime.ShikimoriId ?? 0,
            Title = anime.Title,
            EnglishTitle = anime.TitleEnglish ?? anime.Title,
            Rating = anime.Score ?? 0,
            CoverImage = anime.ImageUrl ?? string.Empty,
            Description = anime.Synopsis ?? string.Empty,
            Year = anime.Year ?? 0,
            Status = AiringStatusLabel.ToLabel(anime.Status),
            Episodes = anime.Episodes ?? 0,
            Genres = anime.Genres,
            CurrentTime = playback.Current,
            TotalTime = playback.Total,
            Progress = playback.Progress,
            WatchStatusSlug = progress is null ? null : WatchStatusSlug.ToSlug(progress.Status)
        };
    }

    public static AnimeDetailDto ToDetail(this Anime anime) =>
        new AnimeWithProgress { Anime = anime }.ToDetail();

    public static BannerAnimeDto ToBanner(this Anime anime) => new()
    {
        Id = anime.Id,
        ShikimoriId = anime.ShikimoriId ?? 0,
        Title = anime.Title,
        EnglishTitle = anime.TitleEnglish ?? anime.Title,
        Rating = anime.Score ?? 0,
        CoverImage = anime.ImageUrl ?? string.Empty
    };

    public static ContinueWatchingDto ToContinueWatching(this ContinueWatchingItem item)
    {
        var playback = Playback(item.Anime, item.Progress);

        return new ContinueWatchingDto
        {
            Id = item.Anime.Id,
            ShikimoriId = item.Anime.ShikimoriId ?? 0,
            Title = item.Anime.Title,
            CoverImage = item.Anime.ImageUrl ?? string.Empty,
            CurrentTime = playback.Current ?? "0:00",
            TotalTime = playback.Total ?? "0:00",
            Progress = playback.Progress ?? 0
        };
    }

    public static NewEpisodeDto ToNewEpisode(this AnimeWithProgress item) => new()
    {
        Id = item.Anime.Id,
        ShikimoriId = item.Anime.ShikimoriId ?? 0,
        Title = item.Anime.Title,
        CoverImage = item.Anime.ImageUrl ?? string.Empty
    };

    public static PagedResultDto<TDto> ToPagedDto<T, TDto>(this PagedResult<T> source, Func<T, TDto> map) => new()
    {
        Items = source.Items.Select(map).ToList(),
        Page = source.Page,
        PageSize = source.PageSize,
        TotalCount = source.TotalCount,
        TotalPages = source.TotalPages,
        HasNext = source.HasNext,
        HasPrevious = source.HasPrevious
    };

    private static (string? Current, string? Total, double? Progress) Playback(Anime anime, UserAnimeProgress? progress)
    {
        if (progress is null)
            return (null, null, null);

        var totalSeconds = progress.LastEpisodeDuration > 0
            ? progress.LastEpisodeDuration
            : DefaultEpisodeSeconds;

        if (progress.LastPlaybackPosition <= 0)
        {
            if (progress.EpisodesWatched <= 0)
                return (null, null, null);

            return (Format(totalSeconds), Format(totalSeconds), 1);
        }

        var currentSeconds = Math.Clamp(progress.LastPlaybackPosition, 0, totalSeconds);
        var ratio = Math.Clamp(currentSeconds / totalSeconds, 0, 1);

        return (Format(currentSeconds), Format(totalSeconds), ratio);
    }

    private static string Format(double seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }
}
