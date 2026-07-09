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
[Authorize(Roles = "Admin,Staff")]
public class ChatBotController : ControllerBase
{
    private readonly IChatBotService _chatBotService;

    public ChatBotController(IChatBotService chatBotService)
    {
        _chatBotService = chatBotService;
    }

    /// <summary>
    /// Ask the cinema assistant a question about workflow or current system data.
    /// </summary>
    [HttpPost("Chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var role = User.FindFirst(ClaimNames.Role)?.Value ?? "Staff";
        var result = await _chatBotService.ChatAsync(request, role);
        return Ok(result);
    }
}
