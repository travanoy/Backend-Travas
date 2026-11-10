using Application.Common;

namespace Application.Services.Abstract;

public interface IPlayerService
{
    Task<Result<KodikPlayerData>> GetSourcesAsync(int shikimoriId, CancellationToken cancellationToken = default);
}

