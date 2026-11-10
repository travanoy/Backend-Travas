using Application.Common;
using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("auth")]
public class AuthController(IAuthService authService, IUserService userService) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshDto? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: "Refresh token is missing.");

        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var result = await userService.GetAsync(CurrentUserId.Value, cancellationToken);
        if (!result.IsSuccess)
            return ToErrorResponse(result);

        return Ok(result.Value!.ToDto());
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult> Logout(RefreshDto? request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request?.RefreshToken, CurrentUserId, cancellationToken);
        return NoContent();
    }

    private static AuthResponseDto ToResponse(AuthResult result) => new()
    {
        AccessToken = result.AccessToken,
        RefreshToken = result.RefreshToken,
        User = result.User.ToDto()
    };
}
