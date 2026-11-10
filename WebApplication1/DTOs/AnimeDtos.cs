namespace WebApplication1.DTOs;

public class AnimeDetailDto
{
    public Guid Id { get; set; }
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EnglishTitle { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string CoverImage { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Episodes { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? CurrentTime { get; set; }
    public string? TotalTime { get; set; }
    public double? Progress { get; set; }
    public string? WatchStatusSlug { get; set; }
}

public class BannerAnimeDto
{
    public Guid Id { get; set; }
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EnglishTitle { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string CoverImage { get; set; } = string.Empty;
}

public class ContinueWatchingDto
{
    public Guid Id { get; set; }
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string CurrentTime { get; set; } = string.Empty;
    public string TotalTime { get; set; } = string.Empty;
    public double Progress { get; set; }
}

public class NewEpisodeDto
{
    public Guid Id { get; set; }
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
}

public class WatchingTypeDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class SetWatchStatusDto
{
    public string StatusSlug { get; set; } = string.Empty;
}
