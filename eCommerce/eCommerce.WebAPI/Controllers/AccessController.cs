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
