using CFMS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Notification management for the authenticated user.
/// </summary>
[Authorize]
[Tags("Notifications")]
public class NotificationsController : BaseController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var result = await _notificationService.GetNotificationsAsync(CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpGet("unread")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetUnreadNotifications(CancellationToken ct)
    {
        var result = await _notificationService.GetUnreadNotificationsAsync(CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _notificationService.GetUnreadNotificationsAsync(CurrentUserId, ct);
        return OkResponse(new { unreadCount = result.Count() });
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken ct)
    {
        await _notificationService.MarkAsReadAsync(id, CurrentUserId, ct);
        return NoContentResponse();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await _notificationService.MarkAllAsReadAsync(CurrentUserId, ct);
        return NoContentResponse();
    }
}
