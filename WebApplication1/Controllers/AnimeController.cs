using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("anime")]
public class AnimeController(IAnimeService animeService, IProgressService progressService) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<AnimeDetailDto>>> GetAll(
        [FromQuery] string? genre,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await animeService.SearchAsync(search, genre, status, page, pageSize, CurrentUserId, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToPagedDto(item => item.ToDetail()));
    }

    [HttpGet("banner")]
    [AllowAnonymous]
    public async Task<ActionResult<BannerAnimeDto>> GetBanner(CancellationToken cancellationToken)
    {
        var result = await animeService.GetBannerAsync(cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToBanner());
    }

    [HttpGet("genres")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<string>>> GetGenres(CancellationToken cancellationToken)
    {
        var result = await animeService.GetGenresAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResponse(result);
    }

    [HttpGet("statuses")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<string>> GetStatuses() => Ok(animeService.GetStatuses());

    [HttpGet("continue-watching")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ContinueWatchingDto>>> GetContinueWatching(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await animeService.GetContinueWatchingAsync(CurrentUserId.Value, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.Select(item => item.ToContinueWatching()).ToList());
    }

    [HttpGet("new-episodes")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<NewEpisodeDto>>> GetNewEpisodes(CancellationToken cancellationToken)
    {
        var result = await animeService.GetNewEpisodesAsync(CurrentUserId, cancellationToken: cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.Select(item => item.ToNewEpisode()).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<AnimeDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await animeService.GetByIdAsync(id, CurrentUserId, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDetail());
    }

    [HttpPut("{id:guid}/watch-status")]
    [Authorize]
    public async Task<ActionResult<AnimeDetailDto>> SetWatchStatus(
        Guid id,
        SetWatchStatusDto request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await progressService.SetWatchStatusAsync(CurrentUserId.Value, id, request.StatusSlug, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDetail());
    }

    [HttpDelete("{id:guid}/watch-status")]
    [Authorize]
    public async Task<ActionResult> RemoveWatchStatus(Guid id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await progressService.RemoveWatchStatusAsync(CurrentUserId.Value, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResponse(result);
    }
}
