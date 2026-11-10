namespace WebApplication1.DTOs;

public class PlayerEpisodeDto
{
    public int Number { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class PlayerTranslationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<PlayerEpisodeDto> Episodes { get; set; } = new();
}

public class PlayerDataDto
{
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<PlayerTranslationDto> Translations { get; set; } = new();
}

public class WatchProgressDto
{
    public int ShikimoriId { get; set; }
    public int Episode { get; set; }
    public int? TranslationId { get; set; }
    public double Position { get; set; }
    public double Duration { get; set; }
}
