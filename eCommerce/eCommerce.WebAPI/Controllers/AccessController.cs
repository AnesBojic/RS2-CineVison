using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Services;
using eCommerce.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers
{
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

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserInsertRequest request)
        {
            await _userService.InsertAsync(request);
            return Ok("You have registered successfully");
        }

        /// <summary>
        /// Sends a 6-digit reset code to the account email (when the account exists).
        /// Always returns the same message so callers cannot probe which accounts exist.
        /// </summary>
        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.ForgotPasswordAsync(request);
            return Ok(new
            {
                message = "If an account exists for that email or username, a reset code has been sent."
            });
        }

        /// <summary>Sets a new password using the emailed reset code.</summary>
        [HttpPost("ResetPassword")]
        [AllowAnonymous]
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
