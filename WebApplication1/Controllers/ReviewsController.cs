using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("anime/{animeId:guid}/reviews")]
public class ReviewsController(IReviewService reviewService) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetAll(Guid animeId, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetForAnimeAsync(animeId, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.Select(review => review.ToDto()).ToList());
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Upsert(Guid animeId, UpsertReviewDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await reviewService.UpsertAsync(CurrentUserId.Value, animeId, request.Rating, request.Text, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDto());
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult> Delete(Guid animeId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await reviewService.DeleteAsync(CurrentUserId.Value, animeId, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResponse(result);
    }
}
