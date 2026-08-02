using eCommerce.Model.Access;

namespace eCommerce.WebAPI.Services.AccessManager
{
    public interface IAccessManager
    {
        Task<UserLoginResponse> LoginAsync(UserLoginRequest request);
        Task<UserLoginResponse> LoginWithRefreshTokenAsync(RefreshAccessTokenRequest request);

        /// <summary>Revokes refresh tokens and bumps token version so existing JWTs fail validation.</summary>
        Task LogoutAsync(int userId);
    }
}
