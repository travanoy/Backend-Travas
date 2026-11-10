using Application.Common;

namespace Application.Interfaces;

public interface IAnilibriaClient
{
    Task<KodikPlayerData?> GetPlayerAsync(
        int shikimoriId,
        IReadOnlyCollection<string?> titles,
        CancellationToken cancellationToken = default);
}
