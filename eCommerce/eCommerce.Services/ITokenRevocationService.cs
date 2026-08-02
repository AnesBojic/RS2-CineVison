namespace eCommerce.Services
{
    /// <summary>
    /// Access tokens are self-contained, so the API has to be told when one stops being
    /// trustworthy. Each user carries a token version that is stamped into the tokens they
    /// are issued; bumping it retires every token handed out before that point.
    /// </summary>
    public interface ITokenRevocationService
    {
        /// <summary>Version to stamp into a token being issued right now.</summary>
        Task<int> GetVersionAsync(int userId);

        /// <summary>
        /// True when the presented token still belongs to a live session: the user exists,
        /// is active, and the token carries the current version.
        /// </summary>
        Task<bool> IsAccessTokenValidAsync(int userId, int tokenVersion);

        /// <summary>
        /// Ends every session for the user — deletes the refresh tokens and retires the
        /// access tokens already issued.
        /// </summary>
        Task RevokeAllSessionsAsync(int userId);

        /// <summary>
        /// Drops the cached snapshot after a caller changed the user row itself, so the
        /// next request reads the new value instead of a stale one.
        /// </summary>
        void InvalidateCache(int userId);
    }
}
