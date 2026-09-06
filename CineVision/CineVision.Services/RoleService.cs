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
        public RoleService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<RoleInsertRequest> insertValidator,
            IValidator<RoleUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
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
            return await base.InsertAsync(request);
        }

        public override async Task<RoleResponse> UpdateAsync(int id, RoleUpdateRequest request)
        {
            var entity = await _dbContext.Roles.FindAsync(id)
                ?? throw new NotFoundException($"Role with id {id} not found.");

            request.Name = (request.Name ?? string.Empty).Trim();
            EnsureNameIsNotReserved(request.Name, existingAuthorizationName: entity.Name);

            // Keep the exact seeded string so "Admin " cannot slip past the rename guard.
            if (IsAuthorizationRole(entity.Name))
            {
                request.Name = entity.Name;
            }

            return await base.UpdateAsync(id, request);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Roles.FindAsync(id)
                ?? throw new NotFoundException($"Role with id {id} not found.");

            if (IsAuthorizationRole(entity.Name))
            {
                throw new ClientException(
                    $"'{entity.Name}' is used by authorization ([Authorize] and JWT role claims) " +
                    "and cannot be deleted.");
            }

            await base.DeleteAsync(id);
        }

        protected override void AfterApplyUsage(RoleResponse response)
        {
            if (!IsAuthorizationRole(response.Name))
            {
                return;
            }

            response.CanDelete = false;
            response.DeleteBlockedReason =
                "Authorization depends on the Admin, Staff, and Customer names, so this role cannot be deleted.";
        }

        /// <summary>
        /// JWT [Authorize(Roles=...)] and RoleNames constants expect these exact strings.
        /// Descriptions may change; the names may not, and new rows must not impersonate them.
        /// </summary>
        private static void EnsureNameIsNotReserved(string? requestedName, string? existingAuthorizationName)
        {
            var trimmed = (requestedName ?? string.Empty).Trim();

            if (IsAuthorizationRole(existingAuthorizationName)
                && !string.Equals(existingAuthorizationName, trimmed, StringComparison.Ordinal))
            {
                throw new ClientException(
                    $"The '{existingAuthorizationName}' role name is used by authorization and cannot be renamed. " +
                    "You can still update the description.");
            }

            if (IsAuthorizationRole(existingAuthorizationName))
            {
                return;
            }

            if (CollidesWithAuthorizationRole(trimmed))
            {
                throw new ClientException(
                    $"'{trimmed}' is reserved for authorization. Use {RoleNames.Admin}, {RoleNames.Staff}, " +
                    $"or {RoleNames.Customer} only for those seeded roles.");
            }
        }

        private static bool IsAuthorizationRole(string? name) =>
            RoleNames.AllRoles.Any(r => string.Equals(r, name, StringComparison.Ordinal));

        private static bool CollidesWithAuthorizationRole(string? name) =>
            RoleNames.AllRoles.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase));
    }
}
