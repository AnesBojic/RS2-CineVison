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
        private readonly ICryptoService _cryptoService;
        private readonly IValidator<UserProfileUpdateRequest> _profileValidator;
        public UserService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<UserInsertRequest> insertValidator, IValidator<UserUpdateRequest> updateValidator, ICryptoService cryptoService, IValidator<UserProfileUpdateRequest> profileValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _cryptoService = cryptoService;
            _profileValidator = profileValidator;
        }


        protected override IEnumerable<User> ApplyFilters(IEnumerable<User> query, UserSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Email))
                {
                    query = query.Where(u => u.Email.Contains(search.Email, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(search.Username))
                {
                    query = query.Where(u => u.Username.Contains(search.Username, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(u => u.FirstName.Contains(search.Name, StringComparison.OrdinalIgnoreCase)
                                          || u.LastName.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
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

        private UserResponse MapUserResponse(User user)
        {
            var response = _mapper.Map<UserResponse>(user);
            response.Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? string.Empty;
            response.ProfileImageBase64 = user.ProfileImageBase64;
            return response;
        }

        private async Task AssignRoleAsync(int userId, string roleName)
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                ?? throw new InvalidOperationException($"Role '{roleName}' was not found.");

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
            IEnumerable<User> query = _dbContext.Users;

            query = await IncludeRelatedEntitiesAsync(search, query.AsQueryable());
            query = ApplyFilters(query, search);

            int? totalCount = null;

            if (search != null)
            {
                if (search.IncludeTotalCount ?? false)
                {
                    totalCount = query.Count();
                }

                if (!string.IsNullOrWhiteSpace(search.SortBy))
                {
                    query = query.AsQueryable().OrderBy(search.SortBy);
                }

                if (search.Page.HasValue && search.PageSize.HasValue)
                {
                    query = query.Skip((search.Page.Value - 1) * search.PageSize.Value);
                }

                if (search.PageSize.HasValue)
                {
                    query = query.Take(search.PageSize.Value);
                }
            }

            var list = query.ToList().Select(MapUserResponse).ToList();

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

            return MapUserResponse(user);
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
                throw new InvalidOperationException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new InvalidOperationException($"Username '{request.Username}' is already in use.");
            }

            var entity = MapInsertRequestToEntity(request);
            entity.CreatedAt = DateTime.UtcNow;

            _dbContext.Users.Add(entity);
            await _dbContext.SaveChangesAsync();

            await AssignRoleAsync(entity.Id, request.Role);

            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
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
                throw new InvalidOperationException($"Email '{request.Email}' is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
            {
                throw new InvalidOperationException($"Username '{request.Username}' is already in use.");
            }

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
                    throw new InvalidOperationException($"Email '{request.Email}' is already in use.");
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
            var entity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"User with id {id} not found.");
            }

            _dbContext.Users.Remove(entity);
            await _dbContext.SaveChangesAsync();
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
               .AsNoTracking()
               .Include(u => u.UserRoles)
               .ThenInclude(ur => ur.Role)
               .FirstOrDefaultAsync(u => u.Id == id);

            UserResponse? response = null;

            if (user != null)
            {
                response = MapUserResponse(user);
            }

            return response;
        }

        public async Task ChangePasswordAsync(UserPasswordChangeRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.Id);

            if (user == null)
                throw new Exception("User not found");

            if (!_cryptoService.Verify(user.PasswordHash, user.PasswordSalt, request.Password))
                throw new Exception("Wrong credential");

            if (!request.NewPassword.Equals(request.ConfirmNewPassword))
                throw new Exception("Password confimation doen't match new password");

            user.PasswordSalt = _cryptoService.GenerateSlat();
            user.PasswordHash = _cryptoService.GenerateHash(request.NewPassword, user.PasswordSalt);


            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}
