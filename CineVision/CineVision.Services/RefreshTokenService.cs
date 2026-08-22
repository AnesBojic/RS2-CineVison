using System.Security.Cryptography;
using System.Text;
using CineVision.Model.Exceptions;
using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly CineVisionDbContext _context;
        private readonly DbSet<RefreshToken> _refreshTokens;

        public RefreshTokenService(CineVisionDbContext context)
        {
            _context = context;
            _refreshTokens = _context.RefreshTokens;
        }

        public async Task<RefreshToken> GetStoredTokenAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var token = await _refreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenHash);

            if (token == null)
            {
                throw new ClientException("Refresh token not found.");
            }

            return token;
        }

        public async Task InsertAsync(RefreshToken refreshToken)
        {
            refreshToken.Token = HashToken(refreshToken.Token);
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public Task DeleteAllUserRefreshTokensAsync(int userId)
        {
            _refreshTokens.RemoveRange(_refreshTokens.Where(rt => rt.UserId == userId));
            return _context.SaveChangesAsync();
        }

        public async Task ReplaceUserTokensAsync(int userId, RefreshToken newToken)
        {
            _refreshTokens.RemoveRange(_refreshTokens.Where(rt => rt.UserId == userId));
            newToken.Token = HashToken(newToken.Token);
            await _refreshTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();
        }

        /// <summary>SHA-256 hash of the raw refresh token (what clients send); only the hash is stored.</summary>
        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
