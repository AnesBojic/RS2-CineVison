using eCommerce.Model.Access;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseCRUDController<UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest, IUserService>
{
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly IEmailService _emailService;

    public UsersController(IUserService userService, IAuthenticatedUserAccessor userAccessor, IEmailService emailService)
        : base(userService)
    {
        _userAccessor = userAccessor;
        _emailService = emailService;
    }

    // Any authenticated user can change their own password.
    [Authorize]
    [HttpPut("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] UserPasswordChangeRequest request)
    {
        await _service.ChangePasswordAsync(request);
        return Ok();
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [Authorize]
    [HttpGet("Me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _service.GetProfileAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>Updates the profile of the currently authenticated user (self-service).</summary>
    [Authorize]
    [HttpPut("Me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> UpdateMe([FromBody] UserProfileUpdateRequest request)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _service.UpdateProfileAsync(userId.Value, request);
        return Ok(result);
    }

    /// <summary>Queues an email to the given user's address (asynchronous delivery via RabbitMQ).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/SendEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEmail(int id, [FromBody] EmailSendRequest request)
    {
        var user = await _service.GetByIdAsync(id);

        await _emailService.QueueEmailAsync(new EmailMessage
        {
            To = user.Email,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml
        });

        return Ok();
    }
}