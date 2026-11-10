using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers;

[Route("player")]
public class PlayerController(IPlayerService playerService) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PlayerDataDto>> GetSources(
        [FromQuery] int shikimoriId,
        CancellationToken cancellationToken)
    {
        var result = await playerService.GetSourcesAsync(shikimoriId, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        var player = result.Value!;

        return Ok(new PlayerDataDto
        {
            ShikimoriId = player.ShikimoriId,
            Title = player.Title,
            Translations = player.Translations
                .Select(translation => new PlayerTranslationDto
                {
                    Id = translation.Id,
                    Title = translation.Title,
                    Type = translation.Type,
                    Episodes = translation.Episodes
                        .Select(episode => new PlayerEpisodeDto
                        {
                            Number = episode.Number,
                            Title = episode.Title,
                            Url = episode.Url
                        })
                        .ToList()
                })
                .ToList()
        });
    }
}
