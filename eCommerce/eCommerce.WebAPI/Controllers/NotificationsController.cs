using eCommerce.Model.Access;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Services.Enums;

namespace eCommerce.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IAuthenticatedUserAccessor _userAccessor;

    public NotificationsController(INotificationService notificationService, IAuthenticatedUserAccessor userAccessor)
    {
        _notificationService = notificationService;
        _userAccessor = userAccessor;
    }

    [HttpGet("UnreadCount")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetAll([FromQuery] int limit = 50)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var items = await _notificationService.GetForUserAsync(userId.Value, limit);
        return Ok(items);
    }

    [HttpPut("{id}/Read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAsReadAsync(userId.Value, id);
        return NoContent();
    }

    [HttpPut("ReadAll")]
    public async Task<IActionResult> MarkAllRead([FromQuery] string? type = null)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        NotificationType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<NotificationType>(type, ignoreCase: true, out var parsed))
            {
                return BadRequest($"Unknown notification type '{type}'.");
            }

            parsedType = parsed;
        }

        await _notificationService.MarkAllReadAsync(userId.Value, parsedType);
        return NoContent();
    }
}
