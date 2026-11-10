using Application.Interfaces;
using Domain.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AnimeRepository(AppDbContext context) : IAnimeRepository
{
    public async Task<IReadOnlyList<Anime>> SearchAsync(
        string? search,
        string? genre,
        AnimeStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.Animes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(anime =>
                anime.Title.Contains(search) ||
                (anime.TitleRussian != null && anime.TitleRussian.Contains(search)) ||
                (anime.TitleEnglish != null && anime.TitleEnglish.Contains(search)));
        }

        if (status is not null)
            query = query.Where(anime => anime.Status == status);

        var result = await query
            .OrderByDescending(anime => anime.Score)
            .ThenBy(anime => anime.Title)
            .ToListAsync(cancellationToken);

       
        if (!string.IsNullOrWhiteSpace(genre))
        {
            result = result
                .Where(anime => anime.Genres.Any(value => string.Equals(value, genre, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return result;
    }

    public async Task<Anime?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Animes.FirstOrDefaultAsync(anime => anime.Id == id, cancellationToken);

    public async Task<Anime?> GetByShikimoriIdAsync(int shikimoriId, CancellationToken cancellationToken = default)
        => await context.Animes.FirstOrDefaultAsync(anime => anime.ShikimoriId == shikimoriId, cancellationToken);

    public async Task<IReadOnlyList<Anime>> GetByShikimoriIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return [];

        return await context.Animes
            .Where(anime => anime.ShikimoriId != null && idList.Contains(anime.ShikimoriId.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<Anime?> GetBannerAsync(CancellationToken cancellationToken = default)
        => await context.Animes
            .AsNoTracking()
            .Where(anime => anime.ImageUrl != null)
            .OrderByDescending(anime => anime.Score)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        var genres = await context.Animes
            .AsNoTracking()
            .Select(anime => anime.Genres)
            .ToListAsync(cancellationToken);

        return genres
            .SelectMany(value => value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
    }

    public async Task<IReadOnlyList<Anime>> GetNewEpisodesAsync(int limit, CancellationToken cancellationToken = default)
        => await context.Animes
            .AsNoTracking()
            .Where(anime => anime.Status == AnimeStatus.CurrentlyAiring)
            .OrderByDescending(anime => anime.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Anime anime, CancellationToken cancellationToken = default)
    {
        if (anime.Id == Guid.Empty)
            anime.Id = Guid.NewGuid();

        await context.Animes.AddAsync(anime, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Anime anime, CancellationToken cancellationToken = default)
    {
        context.Animes.Update(anime);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> UpsertManyAsync(IEnumerable<Anime> animes, CancellationToken cancellationToken = default)
    {
        var incoming = animes
            .Where(anime => anime.ShikimoriId is > 0)
            .GroupBy(anime => anime.ShikimoriId!.Value)
            .Select(group => group.First())
            .ToList();

        if (incoming.Count == 0)
            return 0;

        var ids = incoming.Select(anime => anime.ShikimoriId!.Value).ToList();
        var existing = await context.Animes
            .Where(anime => anime.ShikimoriId != null && ids.Contains(anime.ShikimoriId.Value))
            .ToListAsync(cancellationToken);

        var existingById = existing.ToDictionary(anime => anime.ShikimoriId!.Value);

        foreach (var anime in incoming)
        {
            if (existingById.TryGetValue(anime.ShikimoriId!.Value, out var current))
                Copy(anime, current);
            else
                await context.Animes.AddAsync(anime, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return incoming.Count;
    }

    private static void Copy(Anime source, Anime target)
    {
        target.Title = source.Title;
        target.TitleRussian = source.TitleRussian;
        target.TitleEnglish = source.TitleEnglish;
        target.TitleJapanese = source.TitleJapanese;
        target.TitleSynonyms = source.TitleSynonyms;
        target.ImageUrl = source.ImageUrl ?? target.ImageUrl;
        target.Type = source.Type;
        target.Episodes = source.Episodes ?? target.Episodes;
        target.EpisodesAired = source.EpisodesAired;
        target.NextEpisodeAt = source.NextEpisodeAt ?? target.NextEpisodeAt;
        target.Status = source.Status;
        target.Score = source.Score ?? target.Score;
        target.Year = source.Year ?? target.Year;
        target.Season = source.Season ?? target.Season;
        target.Synopsis = source.Synopsis ?? target.Synopsis;
        target.Genres = source.Genres.Count > 0 ? source.Genres : target.Genres;
        target.Studios = source.Studios.Count > 0 ? source.Studios : target.Studios;
        target.UpdatedAt = DateTime.UtcNow;
    }
}
