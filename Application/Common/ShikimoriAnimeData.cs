namespace Application.Common;

public class ShikimoriAnimeData
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Russian { get; set; }
    public string? English { get; set; }
    public string? Japanese { get; set; }
    public List<string> Synonyms { get; set; } = new();
    public string? Kind { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Episodes { get; set; }
    public int? EpisodesAired { get; set; }
    public DateTime? NextEpisodeAt { get; set; }
    public string? Season { get; set; }
    public int? Year { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Studios { get; set; } = new();
}
