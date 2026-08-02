using eCommerce.Common.Services.CryptoService;
using eCommerce.Model;
using eCommerce.Model.Access;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace eCommerce.WebAPI.Services.AccessManager
{
    public class AccessManager : IAccessManager
    {
        private const int RefreshTokenLifetimeDays = 7;

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ICryptoService _cryptoService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenRevocationService _tokenRevocationService;
        private readonly IValidator<UserLoginRequest> _loginValidator;
        private readonly IValidator<RefreshAccessTokenRequest> _refreshTokenValidator;

        public AccessManager(
            IUserService userService,
            IConfiguration configuration,
            ICryptoService cryptoService,
            IRefreshTokenService refreshTokenService,
            ITokenRevocationService tokenRevocationService,
            IValidator<UserLoginRequest> loginValidator,
            IValidator<RefreshAccessTokenRequest> refreshTokenValidator)
        {
            _userService = userService;
            _configuration = configuration;
            _cryptoService = cryptoService;
            _refreshTokenService = refreshTokenService;
            _tokenRevocationService = tokenRevocationService;
            _loginValidator = loginValidator;
            _refreshTokenValidator = refreshTokenValidator;
        }

        public async Task<UserLoginResponse> LoginAsync(UserLoginRequest request)
        {
            await _loginValidator.ValidateAndThrowAsync(request);

            var user = await _userService.GetByUsernameAsync(request.Username);


            if (user == null)
            {
                throw new ClientException("Invalid username or password.");
            }

            var validPassword = _cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password);
            if (!validPassword)
            {
                throw new ClientException("Invalid username or password.");
            }

            var accessToken = await GenerateTokenAsync(user);
            var refreshTokenValue = GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
            };

            await _refreshTokenService.InsertAsync(refreshToken);

            return new UserLoginResponse
            {
                Accesstoken = accessToken,
                Refreshtoken = refreshTokenValue
            };
        }

        public async Task<UserLoginResponse> LoginWithRefreshTokenAsync(RefreshAccessTokenRequest request)
        {
            await _refreshTokenValidator.ValidateAndThrowAsync(request);

            var refreshToken = await _refreshTokenService.GetStoredTokenAsync(request.RefreshToken);

            if (refreshToken == null)
            {
                throw new ClientException("Invalid refresh token");
            }

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new ClientException("Refresh token has expired");
            }

            var user = await _userService.GetWithRoleByIdAsync(refreshToken.UserId);

            if (user == null)
            {
                throw new ClientException("User not found");
            }

            if (!user.IsActive)
            {
                throw new ClientException("User is not active");
            }

            var accessToken = await GenerateTokenAsync(user);
            var refreshTokenValue = GenerateRefreshToken();

            var token = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
            };

            await _refreshTokenService.ReplaceUserTokensAsync(user.Id, token);

            return new UserLoginResponse
            {
                Accesstoken = accessToken,
                Refreshtoken = refreshTokenValue
            };

        }

        private async Task<string> GenerateTokenAsync(UserResponse user)
        {
            var tokenVersion = await _tokenRevocationService.GetVersionAsync(user.Id);

            string secretKeyString = _configuration["JwtToken:SecretKey"] ?? string.Empty;
            var issuer = _configuration["JwtToken:Issuer"];
            var audience = _configuration["JwtToken:Audience"];
            var durationInMinutes = int.Parse(_configuration["JwtToken:DurationInMinutes"] ?? "1");

            var secretKey = Encoding.ASCII.GetBytes(secretKeyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimNames.Id, user.Id.ToString()),
                    new Claim(ClaimNames.FirstName, user.FirstName ?? string.Empty),
                    new Claim(ClaimNames.LastName, user.LastName ?? string.Empty),
                    new Claim(ClaimNames.Email, user.Email ?? string.Empty),
                    new Claim(ClaimNames.Role, user.Role ?? RoleNames.Customer),
                    new Claim(ClaimNames.IsActive, user.IsActive.ToString()),
                    new Claim(ClaimNames.TokenVersion, tokenVersion.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(durationInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task LogoutAsync(int userId)
        {
            await _tokenRevocationService.RevokeAllSessionsAsync(userId);
        }

        private static string GenerateRefreshToken()
        {
            var randombytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randombytes);
        }

       
    }
}
