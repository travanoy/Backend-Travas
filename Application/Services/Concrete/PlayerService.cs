using Application.Common;
using Application.Interfaces;
using Application.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace Application.Services.Concrete;

public class PlayerService(
    IAnimeRepository animeRepository,
    IShikimoriClient shikimoriClient,
    IAnilibriaClient anilibriaClient,
    IKodikClient kodikClient,
    ILogger<PlayerService> logger) : IPlayerService
{
    public async Task<Result<KodikPlayerData>> GetSourcesAsync(
        int shikimoriId,
        CancellationToken cancellationToken = default)
    {
        if (shikimoriId <= 0)
            return Result<KodikPlayerData>.Failure("Invalid shikimori id.", ErrorType.Validation);

        var anime = await animeRepository.GetByShikimoriIdAsync(shikimoriId, cancellationToken);

        if (anime is null)
        {
            var external = await shikimoriClient.GetByIdAsync(shikimoriId, cancellationToken);
            if (external is not null)
            {
                anime = AnimeMapper.FromShikimori(external);
                await animeRepository.AddAsync(anime, cancellationToken);
            }
        }

        // Основной источник — AniLibria (публичный API, без токена).
        string?[] titles = [anime?.TitleRussian, anime?.Title, anime?.TitleEnglish];
        var player = await anilibriaClient.GetPlayerAsync(shikimoriId, titles, cancellationToken);

        // Резерв — Kodik (если когда-нибудь появится валидный токен).
        if (player is null)
        {
            logger.LogInformation("No AniLibria sources for anime {ShikimoriId}, trying Kodik", shikimoriId);
            player = await kodikClient.GetPlayerAsync(shikimoriId, cancellationToken);
        }

        if (player is null)
        {
            logger.LogWarning("No player sources for anime {ShikimoriId}", shikimoriId);
            player = new KodikPlayerData { ShikimoriId = shikimoriId };
        }

        player.Title = anime?.Title ?? player.Title;
        return Result<KodikPlayerData>.Success(player);
    }
}
