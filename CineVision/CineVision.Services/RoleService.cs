using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services
{
    public class RoleService
        : LookupService<Role, RoleResponse, RoleInsertRequest, RoleUpdateRequest>, IRoleService
    {
        private readonly ITokenRevocationService _tokenRevocationService;
        private List<int>? _pendingSessionInvalidationUserIds;

        public RoleService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<RoleInsertRequest> insertValidator,
            IValidator<RoleUpdateRequest> updateValidator,
            ITokenRevocationService tokenRevocationService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _tokenRevocationService = tokenRevocationService;
        }

        protected override string EntityLabel => "role";

        /// <summary>A small, named set — alphabetical order is more useful than newest first.</summary>
        protected override string? DefaultSortBy => "Name";

        protected override async Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids)
        {
            return await _dbContext.UserRoles
                .Where(ur => ids.Contains(ur.RoleId))
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);
        }

        public override async Task<RoleResponse> InsertAsync(RoleInsertRequest request)
        {
            request.Name = (request.Name ?? string.Empty).Trim();
            EnsureNameIsNotReserved(request.Name, existingAuthorizationName: null);
            RolePermissionMapping.NormalizeColor(request);
            return await base.InsertAsync(request);
        }

        public override async Task<RoleResponse> UpdateAsync(int id, RoleUpdateRequest request)
        {
            var entity = await _dbContext.Roles.FindAsync(id)
                ?? throw new NotFoundException($"Role with id {id} not found.");

            request.Name = (request.Name ?? string.Empty).Trim();
            EnsureNameIsNotReserved(request.Name, existingAuthorizationName: entity.Name);

            if (IsSystemRole(entity.Name))
            {
                request.Name = entity.Name;
            }

            RolePermissionMapping.NormalizeColor(request);
            if (string.Equals(entity.Name, RoleNames.Admin, StringComparison.Ordinal))
            {
                RolePermissionMapping.ApplyFullAccess(request);
            }

            var accessChanged = RolePermissionMapping.Snapshot(entity) != RolePermissionMapping.Snapshot(request);
            if (accessChanged)
            {
                var userIds = await _dbContext.UserRoles
                    .Where(ur => ur.RoleId == id)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                if (userIds.Count > 0)
                {
                    var users = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
                    foreach (var user in users)
                    {
                        user.TokenVersion++;
                    }

                    var tokens = await _dbContext.RefreshTokens
                        .Where(t => userIds.Contains(t.UserId))
                        .ToListAsync();
                    if (tokens.Count > 0)
                    {
                        _dbContext.RefreshTokens.RemoveRange(tokens);
                    }

                    _pendingSessionInvalidationUserIds = userIds;
                }
            }

            var result = await base.UpdateAsync(id, request);

            if (_pendingSessionInvalidationUserIds != null)
            {
                foreach (var userId in _pendingSessionInvalidationUserIds)
                {
                    _tokenRevocationService.InvalidateUserSessions(userId);
                }

                _pendingSessionInvalidationUserIds = null;
            }

            return result;
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Roles.FindAsync(id)
                ?? throw new NotFoundException($"Role with id {id} not found.");

            if (IsSystemRole(entity.Name))
            {
                throw new ClientException(
                    $"'{entity.Name}' is a system role and cannot be deleted.");
            }

            await base.DeleteAsync(id);
        }

        protected override void AfterApplyUsage(RoleResponse response)
        {
            response.IsSystemRole = IsSystemRole(response.Name);
            response.PermissionsLocked = string.Equals(response.Name, RoleNames.Admin, StringComparison.Ordinal);

            if (!response.IsSystemRole)
            {
                return;
            }

            response.CanDelete = false;
            response.DeleteBlockedReason =
                "Admin and Customer are system roles and cannot be deleted.";
        }

        /// <summary>
        /// Admin and Customer names stay unique and frozen. Staff is optional.
        /// What a role can do is controlled by permission flags, not the name.
        /// </summary>
        private static void EnsureNameIsNotReserved(string? requestedName, string? existingAuthorizationName)
        {
            var trimmed = (requestedName ?? string.Empty).Trim();

            if (IsSystemRole(existingAuthorizationName)
                && !string.Equals(existingAuthorizationName, trimmed, StringComparison.Ordinal))
            {
                throw new ClientException(
                    $"The '{existingAuthorizationName}' role name is reserved and cannot be renamed. " +
                    "You can still update the description, color, and permissions.");
            }

            if (IsSystemRole(existingAuthorizationName))
            {
                return;
            }

            if (CollidesWithSystemRole(trimmed))
            {
                throw new ClientException(
                    $"'{trimmed}' is reserved. Use {RoleNames.Admin} or {RoleNames.Customer} " +
                    "only for those system roles.");
            }
        }

        private static bool IsSystemRole(string? name) => RoleNames.IsSystemRole(name);

        private static bool CollidesWithSystemRole(string? name) =>
            RoleNames.SystemRoles.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase));
    }
}
