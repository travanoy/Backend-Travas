using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("saved")]
[Authorize]
public class SavedController(IAnimeService animeService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AnimeDetailDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await animeService.GetSavedAsync(CurrentUserId.Value, status, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToPagedDto(item => item.ToDetail()));
    }
}
