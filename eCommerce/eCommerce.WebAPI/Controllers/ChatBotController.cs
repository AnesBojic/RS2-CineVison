using eCommerce.Model;
using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Internal cinema assistant for staff and administrators. Uses OpenAI with a live data snapshot.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = RoleNames.AdminStaff)]
public class ChatBotController : ControllerBase
{
    private readonly IChatBotService _chatBotService;
    private readonly INotificationService _notificationService;
    private readonly IAuthenticatedUserAccessor _userAccessor;

    public ChatBotController(
        IChatBotService chatBotService,
        INotificationService notificationService,
        IAuthenticatedUserAccessor userAccessor)
    {
        _chatBotService = chatBotService;
        _notificationService = notificationService;
        _userAccessor = userAccessor;
    }

    /// <summary>
    /// Ask the cinema assistant a question about workflow or current system data.
    /// </summary>
    [HttpPost("Chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var role = User.FindFirst(ClaimNames.Role)?.Value ?? RoleNames.Staff;
        var result = await _chatBotService.ChatAsync(request, role);

        var userId = _userAccessor.GetUserId();
        if (userId != null && !string.IsNullOrWhiteSpace(result.Reply))
        {
            var preview = result.Reply.Length > 180 ? result.Reply[..180] + "…" : result.Reply;
            await _notificationService.CreateAsync(
                userId.Value,
                "Cinema Assistant replied",
                preview,
                "Message");
        }

        return Ok(result);
    }
}
