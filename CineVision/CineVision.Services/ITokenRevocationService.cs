namespace CineVision.Services
{
    /// <summary>
    /// Tracks per-user JWT version so logout, deactivation, and role changes invalidate issued access tokens.
    /// </summary>
    public interface ITokenRevocationService
    {
        Task<int> GetVersionAsync(int userId);

        Task<bool> IsAccessTokenValidAsync(int userId, int tokenVersion);

        Task RevokeAllSessionsAsync(int userId);

        /// <summary>
        /// Drops the cached token state and closes the user's live SignalR connections. Call this
        /// after saving a change that bumped <c>TokenVersion</c> outside this service.
        /// </summary>
        void InvalidateUserSessions(int userId);
    }
}
