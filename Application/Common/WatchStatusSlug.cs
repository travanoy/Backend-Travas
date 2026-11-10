using Domain.Entity;

namespace Application.Common;

public static class WatchStatusSlug
{
    public record Option(string Slug, string Title, WatchingEnum Status);

    public static readonly IReadOnlyList<Option> All =
    [
        new("watching", "Watching", WatchingEnum.Watching),
        new("completed", "Completed", WatchingEnum.Completed),
        new("on-hold", "On hold", WatchingEnum.OnHold),
        new("dropped", "Dropped", WatchingEnum.Dropped),
        new("planned", "Planned", WatchingEnum.Planned)
    ];

    public static string ToSlug(WatchingEnum status) =>
        All.First(option => option.Status == status).Slug;

    public static string ToTitle(WatchingEnum status) =>
        All.First(option => option.Status == status).Title;

    public static bool TryParse(string? slug, out WatchingEnum status)
    {
        var option = All.FirstOrDefault(item =>
            string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));

        status = option?.Status ?? WatchingEnum.Planned;
        return option is not null;
    }
}
