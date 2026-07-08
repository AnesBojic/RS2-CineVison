using eCommerce.Model.Access;

namespace eCommerce.WebAPI.Services.AccessManager
{
    public interface IAccessManager
    {
        Task<UserLoginResponse> LoginAsync(UserLoginRequest request);
        Task<UserLoginResponse> LoginWithRefreshTokenAsync(RefreshAccessTokenRequest request);

        /// <summary>
        /// Logs the user out by revoking all of their stored refresh tokens so they can no
        /// longer obtain new access tokens. The current access token remains valid until it expires.
        /// </summary>
        Task LogoutAsync(int userId);
    }
}
