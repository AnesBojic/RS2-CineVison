using eCommerce.Services.Database;


namespace eCommerce.Services
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> GetStoredTokenAsync(string refreshToken);
        Task InsertAsync(RefreshToken refreshToken);
        Task DeleteAllUserRefreshTokensAsync(int userId);

        /// <summary>
        /// Revokes the user's existing refresh tokens and stores <paramref name="newToken"/> in a
        /// single save, so a rotation can never leave the user with no usable token.
        /// </summary>
        Task ReplaceUserTokensAsync(int userId, RefreshToken newToken);
    }
}
