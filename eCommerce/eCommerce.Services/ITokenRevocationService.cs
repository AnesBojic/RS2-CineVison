namespace eCommerce.Services
{
    /// <summary>Tracks per-user JWT version so logout can invalidate issued access tokens.</summary>
    public interface ITokenRevocationService
    {
        Task<int> GetVersionAsync(int userId);

        Task<bool> IsAccessTokenValidAsync(int userId, int tokenVersion);

        Task RevokeAllSessionsAsync(int userId);

        void InvalidateCache(int userId);
    }
}
