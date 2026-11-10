using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("users")]
[Authorize]
public class UserController(IUserService userService) : ApiControllerBase
{
    [HttpPatch("me")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: "Name is required.");

        var result = await userService.UpdateNameAsync(CurrentUserId.Value, request.Name, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDto());
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile? avatar, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        if (avatar is null || avatar.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: "Avatar file is required.");

        if (avatar.Length > 5 * 1024 * 1024)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: "Avatar must be 5 MB or less.");

        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: "Only jpg, jpeg, png and webp images are allowed.");

        await using var stream = avatar.OpenReadStream();
        var result = await userService.UpdateAvatarAsync(CurrentUserId.Value, stream, avatar.FileName, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDto());
    }

    [HttpPut("me/push-token")]
    public async Task<ActionResult> SetPushToken(PushTokenDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await userService.SetPushTokenAsync(CurrentUserId.Value, request.ExpoPushToken, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResponse(result);
    }

    [HttpGet("me/stats")]
    public async Task<ActionResult<UserStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await userService.GetStatsAsync(CurrentUserId.Value, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDto());
    }
}
