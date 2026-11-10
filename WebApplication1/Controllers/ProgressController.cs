using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers;

[Route("progress")]
[Authorize]
public class ProgressController(IProgressService progressService) : ApiControllerBase
{
    [HttpPost]
    [HttpPatch]
    public async Task<ActionResult> Save(WatchProgressDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await progressService.SaveProgressAsync(
            CurrentUserId.Value,
            request.ShikimoriId,
            request.Episode,
            request.TranslationId,
            request.Position,
            request.Duration,
            cancellationToken);

        return result.IsSuccess ? NoContent() : ToErrorResponse(result);
    }
}
