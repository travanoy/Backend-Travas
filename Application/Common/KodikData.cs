namespace Application.Common;

public class KodikEpisodeData
{
    public int Number { get; set; }
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class KodikTranslationData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<KodikEpisodeData> Episodes { get; set; } = new();
}

public class KodikPlayerData
{
    public int ShikimoriId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<KodikTranslationData> Translations { get; set; } = new();
}

public class KodikUpdatedItem
{
    public int ShikimoriId { get; set; }
    public int? LastEpisode { get; set; }
}
