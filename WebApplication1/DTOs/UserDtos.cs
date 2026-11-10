namespace WebApplication1.DTOs;

public class UpdateProfileDto
{
    public string? Name { get; set; }
}

public class PushTokenDto
{
    public string? ExpoPushToken { get; set; }
}

public class UserStatsDto
{
    public int Watching { get; set; }
    public int Completed { get; set; }
    public int Planned { get; set; }
    public int OnHold { get; set; }
    public int Dropped { get; set; }
    public int TotalAnime { get; set; }
    public int EpisodesWatched { get; set; }
    public double DaysWatched { get; set; }
}
