namespace Application.Services.Abstract;

public interface ICatalogService
{
    Task SyncCatalogAsync(CancellationToken cancellationToken = default);
    Task CheckNewEpisodesAsync(CancellationToken cancellationToken = default);
    Task CheckNewEpisodesFromShikimoriAsync(CancellationToken cancellationToken = default);
}
