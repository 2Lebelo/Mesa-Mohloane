using Mesa_Mohloane_Backend.Services;
using Mesa_Mohloane_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notifications) : ControllerBase
{
    private readonly INotificationService _notifications = notifications;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        var result = await _notifications.GetMyNotificationsAsync(
            CurrentUserId, page, pageSize, unreadOnly);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notifications.GetUnreadCountAsync(CurrentUserId);
        return Ok(new { unreadCount = count });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _notifications.MarkAsReadAsync(id, CurrentUserId);
        return result.Success
            ? Ok(new { message = "Notification marked as read." })
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(CurrentUserId);
        return Ok(new { message = "All notifications marked as read." });
    }
}