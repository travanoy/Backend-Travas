namespace Domain.Entity;

public class Anime
{
    public Guid Id { get; set; }

    
    
    public int? ShikimoriId { get; set; }  //for anime players kodic etc.
    public int?  MalId { get; set; }
    
    
    public required string Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public string? TitleRussian { get; set; }
    public List<string> TitleSynonyms { get; set; } = new();

   
    public string? ImageUrl { get; set; }
    public AnimeType Type { get; set; } 
    public int? Episodes { get; set; }
    public AnimeStatus Status { get; set; } 
    public double? Score { get; set; }
    public int? Year { get; set; }
    public string? Season { get; set; }
    public string? Synopsis { get; set; }
    
    public List<string> Genres { get; set; } = new();
    public List<string> Studios { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<UserAnimeProgress> UserProgresses { get; set; } = new List<UserAnimeProgress>();
}