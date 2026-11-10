using System.Security.Claims;
using Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers.Base;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    protected ActionResult ToErrorResponse(Result result)
    {
        var (statusCode, title) = result.ErrorType switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Operation conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Authentication failed"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Access denied"),
            _ => (StatusCodes.Status400BadRequest, "Request failed")
        };

        return Problem(statusCode: statusCode, title: title, detail: result.Error);
    }
}
