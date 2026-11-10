using Domain.Entity;

namespace Application.Interfaces;

public interface IAnimeRepository
{
    Task<IReadOnlyList<Anime>> SearchAsync(
        string? search,
        string? genre,
        AnimeStatus? status,
        CancellationToken cancellationToken = default);

    Task<Anime?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Anime?> GetByShikimoriIdAsync(int shikimoriId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Anime>> GetByShikimoriIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task<Anime?> GetBannerAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Anime>> GetNewEpisodesAsync(int limit, CancellationToken cancellationToken = default);

    Task AddAsync(Anime anime, CancellationToken cancellationToken = default);
    Task UpdateAsync(Anime anime, CancellationToken cancellationToken = default);

    // Атомарный upsert по ShikimoriId: обновляет существующие, добавляет новые.
    Task<int> UpsertManyAsync(IEnumerable<Anime> animes, CancellationToken cancellationToken = default);
}
