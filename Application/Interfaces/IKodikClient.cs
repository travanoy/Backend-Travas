using Application.Common;

namespace Application.Interfaces;

public interface IKodikClient
{
    Task<KodikPlayerData?> GetPlayerAsync(int shikimoriId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KodikUpdatedItem>> GetRecentlyUpdatedAsync(int limit, CancellationToken cancellationToken = default);
}
