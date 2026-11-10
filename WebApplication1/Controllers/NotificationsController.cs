using Application.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;
using WebApplication1.Mappings;

namespace WebApplication1.Controllers;

[Route("notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<NotificationDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await notificationService.GetAsync(CurrentUserId.Value, page, pageSize, cancellationToken);
        return Ok(result.ToPagedDto(notification => notification.ToDto()));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var count = await notificationService.GetUnreadCountAsync(CurrentUserId.Value, cancellationToken);
        return Ok(new UnreadCountDto { Count = count });
    }

    [HttpPost("read")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        await notificationService.MarkAllReadAsync(CurrentUserId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
            return Unauthorized();

        var marked = await notificationService.MarkReadAsync(CurrentUserId.Value, id, cancellationToken);
        return marked ? NoContent() : NotFound();
    }
}
