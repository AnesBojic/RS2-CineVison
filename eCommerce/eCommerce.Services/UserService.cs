using eCommerce.Model;
using eCommerce.Model.Messages;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Access;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public class UserService : BaseCRUDService<User, UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest>, IUserService
    {
        /// <summary>~300KB of base64; keeps API/list/edit payloads usable.</summary>
        private const int MaxProfileImageBase64Length = 400_000;

        private readonly ICryptoService _cryptoService;
        private readonly IValidator<UserProfileUpdateRequest> _profileValidator;
        private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
        private readonly IValidator<UserRegisterRequest> _registerValidator;
        private readonly IValidator<UserPasswordChangeRequest> _passwordChangeValidator;
        private readonly IEmailService _emailService;
        private readonly ITokenRevocationService _tokenRevocationService;

        public UserService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<UserInsertRequest> insertValidator,
            IValidator<UserUpdateRequest> updateValidator,
            ICryptoService cryptoService,
            IValidator<UserProfileUpdateRequest> profileValidator,
            IValidator<ForgotPasswordRequest> forgotPasswordValidator,
            IValidator<ResetPasswordRequest> resetPasswordValidator,
            IValidator<UserRegisterRequest> registerValidator,
            IValidator<UserPasswordChangeRequest> passwordChangeValidator,
            IEmailService emailService,
            ITokenRevocationService tokenRevocationService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _tokenRevocationService = tokenRevocationService;
            _cryptoService = cryptoService;
            _profileValidator = profileValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _registerValidator = registerValidator;
            _passwordChangeValidator = passwordChangeValidator;
            _emailService = emailService;
        }


        protected override IQueryable<User> ApplyFilters(IQueryable<User> query, UserSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Email))
                {
                    var email = search.Email;
                    query = query.Where(u => u.Email.Contains(email));
                }

                if (!string.IsNullOrWhiteSpace(search.Username))
                {
                    var username = search.Username;
                    query = query.Where(u => u.Username.Contains(username));
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name;
                    query = query.Where(u => u.FirstName.Contains(name) || u.LastName.Contains(name));
                }

                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive.Value);
                }
            }

            return query;
        }

        protected override async Task<IQueryable<User>> IncludeRelatedEntitiesAsync(UserSearch? search, IQueryable<User> query)
        {
            return query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        }

        private UserResponse MapUserResponse(User user, bool includeProfileImage = true)
        {
            var response = _mapper.Map<UserResponse>(user);
            response.Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? string.Empty;
            response.ProfileImageBase64 = includeProfileImage ? user.ProfileImageBase64 : null;
            return response;
        }

        private static void EnsureProfileImageSize(string? profileImageBase64)
        {
            if (!string.IsNullOrEmpty(profileImageBase64) &&
                profileImageBase64.Length > MaxProfileImageBase64Length)
            {
                throw new ClientException(
                    "Profile photo is too large. Please upload a smaller image (under ~300 KB).");
            }
        }

        private async Task ClearOversizedProfileImageIfNeededAsync(User user)
        {
            if (string.IsNullOrEmpty(user.ProfileImageBase64) ||
                user.ProfileImageBase64.Length <= MaxProfileImageBase64Length)
            {
                return;
            }

            user.ProfileImageBase64 = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        private async Task AssignRoleAsync(int userId, string roleName)
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                ?? throw new ClientException($"Role '{roleName}' was not found.");

            var existingRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            if (existingRoles.Count == 1 && existingRoles[0].RoleId == role.Id)
            {
                return;
            }

            _dbContext.UserRoles.RemoveRange(existingRoles);
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.Id,
                DateAssigned = DateTime.UtcNow
            });
        }

        public override async Task<PageResult<UserResponse>> GetAllAsync(UserSearch? search = null)
        {
            search ??= new UserSearch();
            PagingLimits.Normalize(search);

            IQueryable<User> query = _dbContext.Users.AsNoTracking();
            query = await IncludeRelatedEntitiesAsync(search, query);
            query = ApplyFilters(query, search);

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            // Newest first by default so an account created a moment ago is the first row.
            query = query.OrderBy(string.IsNullOrWhiteSpace(search.SortBy) ? "Id desc" : search.SortBy);

            query = query
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var entities = await query.ToListAsync();
            var list = entities.Select(u => MapUserResponse(u, includeProfileImage: false)).ToList();

            return new PageResult<UserResponse>
            {
                Items = list,
                TotalCount = totalCount
            };
        }

        public override async Task<UserResponse> GetByIdAsync(int id)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            // Oversized photos break desktop edit/email payloads — drop them so the user stays manageable.
            await ClearOversizedProfileImageIfNeededAsync(user);

            return MapUserResponse(user);
        }

        public async Task<string> GetEmailByIdAsync(int id)
        {
            var email = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            return email;
        }

        protected override User MapInsertRequestToEntity(UserInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);

            // Handle password hashing for User entity
            var salt = _cryptoService.GenerateSlat();
            entity.PasswordSalt = salt;
            entity.PasswordHash = _cryptoService.GenerateHash(request.Password, salt);

            return entity;
        }

        public override async Task<UserResponse> InsertAsync(UserInsertRequest request)
        {
            // let FluentValidation throw if the request isn't valid; the exception filter will
            // convert the resulting ValidationException into the standard error format.
            await _insertValidator.ValidateAndThrowAsync(request);

            // Check if email or username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new ClientException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new ClientException($"Username '{request.Username}' is already in use.");
            }

            EnsureProfileImageSize(request.ProfileImageBase64);
            if (!string.IsNullOrWhiteSpace(request.ProfileImageBase64)
                && !ImageContentValidator.TryValidateBase64(request.ProfileImageBase64, out _, out var imageError))
            {
                throw new ClientException(imageError);
            }

            var entity = MapInsertRequestToEntity(request);
            entity.CreatedAt = DateTime.UtcNow;

            // The role row needs the generated user id, so this cannot be a single save.
            // Both writes share a transaction so a failure can't leave a user without a role.
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.Users.Add(entity);
                await _dbContext.SaveChangesAsync();

                await AssignRoleAsync(entity.Id, request.Role);
                await _dbContext.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return await GetByIdAsync(entity.Id);
        }

        public async Task<UserResponse> RegisterAsync(UserRegisterRequest request)
        {
            await _registerValidator.ValidateAndThrowAsync(request);

            // Role is never taken from the client for public registration.
            return await InsertAsync(new UserInsertRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber,
                ProfileImageBase64 = request.ProfileImageBase64,
                Role = RoleNames.Customer,
                IsActive = true
            });
        }


        public override async Task<UserResponse> UpdateAsync(int id, UserUpdateRequest request)
        {
            await _updateValidator.ValidateAndThrowAsync(request);

            var entity = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            // Check if email or username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                throw new ClientException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
            {
                throw new ClientException($"Username '{request.Username}' is already in use.");
            }

            EnsureProfileImageSize(request.ProfileImageBase64);

            MapUpdateRequestToEntity(request, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                await AssignRoleAsync(id, request.Role);
            }

            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<UserResponse> GetProfileAsync(int userId)
        {
            var response = await GetWithRoleByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User with id {userId} not found.");
            return response;
        }

        public async Task<UserResponse> UpdateProfileAsync(int userId, UserProfileUpdateRequest request)
        {
            await _profileValidator.ValidateAndThrowAsync(request);

            var entity = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {userId} not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Equals(entity.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId))
                {
                    throw new ClientException($"Email '{request.Email}' is already in use.");
                }
                entity.Email = request.Email;
            }

            // Only self-service editable fields; role, IsActive, username and password are never touched here.
            if (request.FirstName != null)
            {
                entity.FirstName = request.FirstName;
            }
            if (request.LastName != null)
            {
                entity.LastName = request.LastName;
            }
            if (request.PhoneNumber != null)
            {
                entity.PhoneNumber = request.PhoneNumber;
            }
            if (request.ProfileImageBase64 != null)
            {
                EnsureProfileImageSize(request.ProfileImageBase64);
                if (!string.IsNullOrWhiteSpace(request.ProfileImageBase64)
                    && !ImageContentValidator.TryValidateBase64(request.ProfileImageBase64, out _, out var imageError))
                {
                    throw new ClientException(imageError);
                }

                entity.ProfileImageBase64 = request.ProfileImageBase64;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var response = _mapper.Map<UserResponse>(entity);
            response.Role = entity.UserRoles.FirstOrDefault()?.Role.Name ?? string.Empty;
            response.ProfileImageBase64 = entity.ProfileImageBase64;
            return response;
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            // Reservations / reviews use Restrict — remove related details so admin delete succeeds.
            var reservations = await _dbContext.Reservations
                .Include(r => r.ReservationSeats)
                .Where(r => r.UserId == id)
                .ToListAsync();
            if (reservations.Count > 0)
            {
                _dbContext.Reservations.RemoveRange(reservations);
            }

            var reviews = await _dbContext.Reviews.Where(r => r.UserId == id).ToListAsync();
            if (reviews.Count > 0)
            {
                _dbContext.Reviews.RemoveRange(reviews);
            }

            var notifications = await _dbContext.UserNotifications.Where(n => n.UserId == id).ToListAsync();
            if (notifications.Count > 0)
            {
                _dbContext.UserNotifications.RemoveRange(notifications);
            }

            if (entity.RefreshTokens.Count > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(entity.RefreshTokens);
            }

            if (entity.UserRoles.Count > 0)
            {
                _dbContext.UserRoles.RemoveRange(entity.UserRoles);
            }

            _dbContext.Users.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new KeyNotFoundException($"User with id {id} not found.");

            return new UserDeleteImpactResponse
            {
                UserId = user.Id,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                ReservationCount = await _dbContext.Reservations.CountAsync(r => r.UserId == id),
                ReviewCount = await _dbContext.Reviews.CountAsync(r => r.UserId == id),
                NotificationCount = await _dbContext.UserNotifications.CountAsync(n => n.UserId == id),
            };
        }

        public async Task<UserSensitveResponse?> GetByUsernameAsync(string username)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            UserSensitveResponse? response = null;

            if (user != null)
            {
                response = _mapper.Map<UserSensitveResponse>(user);
                response.Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? string.Empty;
            }

            return response;
        }

        public async Task<UserResponse?> GetWithRoleByIdAsync(int id)
        {
            var user = await _dbContext.Users
               .Include(u => u.UserRoles)
               .ThenInclude(ur => ur.Role)
               .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            await ClearOversizedProfileImageIfNeededAsync(user);
            return MapUserResponse(user);
        }

        public async Task ChangePasswordAsync(UserPasswordChangeRequest request)
        {
            await _passwordChangeValidator.ValidateAndThrowAsync(request);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id)
                ?? throw new ClientException("User not found.");

            if (!_cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password))
                throw new ClientException("Current password is incorrect.");

            user.PasswordSalt = _cryptoService.GenerateSlat();
            user.PasswordHash = _cryptoService.GenerateHash(request.NewPassword, user.PasswordSalt);

            // Other devices have to sign in again with the new password. The caller keeps its
            // own access token, so changing the password does not eject them mid-session.
            var tokens = await _dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
            if (tokens.Count > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(tokens);
            }

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            await _forgotPasswordValidator.ValidateAndThrowAsync(request);

            var key = request.EmailOrUsername.Trim();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                u.Email == key || u.Username == key);

            // Always succeed from the caller's perspective to avoid account enumeration.
            if (user == null || !user.IsActive)
            {
                return;
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            // Store a hash of the code (never plain text). Email still receives the raw code.
            user.PasswordResetCode = _cryptoService.GenerateHash(code, user.PasswordSalt);
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _emailService.QueueEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = "CineVision password reset code",
                Body =
                    $"Hello {user.FirstName},\n\n" +
                    $"Your password reset code is: {code}\n\n" +
                    "This code expires in 15 minutes.\n" +
                    "If you did not request a reset, you can ignore this email.\n\n" +
                    "CineVision",
                IsHtml = false
            });
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            await _resetPasswordValidator.ValidateAndThrowAsync(request);

            var key = request.EmailOrUsername.Trim();
            var code = request.Code.Trim();

            var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                u.Email == key || u.Username == key);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordResetCode) ||
                !_cryptoService.Verify(user.PasswordResetCode, user.PasswordSalt, code) ||
                !user.PasswordResetExpiresAt.HasValue ||
                user.PasswordResetExpiresAt.Value < DateTime.UtcNow)
            {
                throw new ClientException("Invalid or expired reset code.");
            }

            user.PasswordSalt = _cryptoService.GenerateSlat();
            user.PasswordHash = _cryptoService.GenerateHash(request.NewPassword, user.PasswordSalt);
            user.PasswordResetCode = null;
            user.PasswordResetExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            // A reset means the account may have been compromised, and whoever runs it is not
            // signed in anyway, so every session goes: refresh tokens and access tokens alike.
            user.TokenVersion++;
            var tokens = await _dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
            if (tokens.Count > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(tokens);
            }

            await _dbContext.SaveChangesAsync();
            _tokenRevocationService.InvalidateCache(user.Id);
        }
    }
}
