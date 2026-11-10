using Domain.Entity;

namespace Application.Common;

public static class AiringStatusLabel
{
    public static string ToLabel(AnimeStatus status) => status switch
    {
        AnimeStatus.CurrentlyAiring => "Ongoing",
        AnimeStatus.NotYetAired => "Upcoming",
        _ => "Completed"
    };

    public static bool TryParse(string? value, out AnimeStatus status)
    {
        status = AnimeStatus.FinishedAiring;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();

        switch (normalized)
        {
            case "ongoing":
            case "currentlyairing":
            case "airing":
                status = AnimeStatus.CurrentlyAiring;
                return true;
            case "upcoming":
            case "notyetaired":
            case "anons":
                status = AnimeStatus.NotYetAired;
                return true;
            case "completed":
            case "finished":
            case "finishedairing":
            case "released":
                status = AnimeStatus.FinishedAiring;
                return true;
            default:
                return false;
        }
    }
}
