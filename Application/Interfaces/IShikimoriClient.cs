using Application.Common;

namespace Application.Interfaces;

public interface IShikimoriClient
{
    Task<IReadOnlyList<ShikimoriAnimeData>> SearchAsync(string search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShikimoriAnimeData>> GetPopularAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShikimoriAnimeData>> GetOngoingAsync(int limit, CancellationToken cancellationToken = default);
    Task<ShikimoriAnimeData?> GetByIdAsync(int shikimoriId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShikimoriAnimeData>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
