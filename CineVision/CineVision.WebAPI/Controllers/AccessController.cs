using CineVision.Model.Access;
using CineVision.Model.Requests;
using CineVision.Services;
using CineVision.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers
{
    /// <summary>
    /// Auth endpoints. <see cref="AllowAnonymousAttribute"/> only on Login/Register;
    /// Forgot/Reset/Refresh stay public (no class-level authorize).
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AccessController : Controller
    {
        private readonly IAccessManager _accessManager;
        private readonly IUserService _userService;
        private readonly IAuthenticatedUserAccessor _userAccessor;

        public AccessController(IAccessManager accessManager, IUserService userService, IAuthenticatedUserAccessor userAccessor)
        {
            _accessManager = accessManager;
            _userService = userService;
            _userAccessor = userAccessor;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] UserLoginRequest request)
        {
            var result = await _accessManager.LoginAsync(request);
            return Ok(result);
        }

        [HttpPost("LoginWithRefreshToken")]
        public async Task<ActionResult> LoginWithRefreshToken([FromBody] RefreshAccessTokenRequest request)
        {
            var result = await _accessManager.LoginWithRefreshTokenAsync(request);
            return Ok(result);
        }

        /// <summary>Public registration; role is always Customer.</summary>
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            await _userService.RegisterAsync(request);
            return Ok("You have registered successfully");
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.ForgotPasswordAsync(request);
            return Ok(new
            {
                message = "If an account exists for that email or username, a reset code has been sent."
            });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(request);
            return Ok(new { message = "Password has been reset. You can sign in with your new password." });
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = _userAccessor.GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            await _accessManager.LogoutAsync(userId.Value);
            return Ok("You have logged out successfully");
        }
    }
}
