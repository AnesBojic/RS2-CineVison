using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>Self-service profile endpoints available to any authenticated user.</summary>
[ApiController]
[Route("Users")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticatedUserAccessor _userAccessor;

    public UserProfileController(IUserService userService, IAuthenticatedUserAccessor userAccessor)
    {
        _userService = userService;
        _userAccessor = userAccessor;
    }

    [HttpPut("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] UserPasswordChangeRequest request)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        request.Id = userId.Value;
        await _userService.ChangePasswordAsync(request);
        return Ok();
    }

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

        var result = await _userService.GetProfileAsync(userId.Value);
        return Ok(result);
    }

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

        var result = await _userService.UpdateProfileAsync(userId.Value, request);
        return Ok(result);
    }
}
