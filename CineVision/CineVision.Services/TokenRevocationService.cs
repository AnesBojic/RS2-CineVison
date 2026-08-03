using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CineVision.Services
{
    public class TokenRevocationService : ITokenRevocationService
    {
        /// <summary>
        /// The version is read on every authenticated request, so it is cached. The window is
        /// short enough that a change made outside this service still takes effect quickly;
        /// changes made through it evict the entry immediately.
        /// </summary>
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);

        private readonly CineVisionDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public TokenRevocationService(CineVisionDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<int> GetVersionAsync(int userId)
        {
            var snapshot = await GetSnapshotAsync(userId);
            return snapshot?.TokenVersion ?? 0;
        }

        public async Task<bool> IsAccessTokenValidAsync(int userId, int tokenVersion)
        {
            var snapshot = await GetSnapshotAsync(userId);
            return snapshot is not null
                && snapshot.IsActive
                && snapshot.TokenVersion == tokenVersion;
        }

        public async Task RevokeAllSessionsAsync(int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return;
            }

            user.TokenVersion++;

            var tokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
            if (tokens.Count > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(tokens);
            }

            // A single save keeps the version bump and the refresh-token cleanup atomic.
            await _dbContext.SaveChangesAsync();
            InvalidateCache(userId);
        }

        public void InvalidateCache(int userId) => _cache.Remove(CacheKey(userId));

        private async Task<UserTokenSnapshot?> GetSnapshotAsync(int userId)
        {
            if (_cache.TryGetValue(CacheKey(userId), out UserTokenSnapshot? cached))
            {
                return cached;
            }

            var snapshot = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserTokenSnapshot(u.TokenVersion, u.IsActive))
                .FirstOrDefaultAsync();

            _cache.Set(CacheKey(userId), snapshot, CacheLifetime);
            return snapshot;
        }

        private static string CacheKey(int userId) => $"user-token-state:{userId}";

        private sealed record UserTokenSnapshot(int TokenVersion, bool IsActive);
    }
}
