using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services
{
    /// <summary>
    /// Shared CRUD behaviour for the reference (lookup) tables. On top of the generic base it
    /// adds name uniqueness, usage counting and a friendly block when a row is still referenced.
    /// </summary>
    public abstract class LookupService<TEntity, TResponse, TInsertRequest, TUpdateRequest>
        : BaseCRUDService<TEntity, TResponse, LookupSearchObject, TInsertRequest, TUpdateRequest>
        where TEntity : class, ILookupEntity
        where TResponse : LookupResponse
        where TInsertRequest : LookupRequest
        where TUpdateRequest : LookupRequest
    {
        protected LookupService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<TInsertRequest> insertValidator,
            IValidator<TUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        /// <summary>Lower-case singular label used in user-facing messages, e.g. "screen type".</summary>
        protected abstract string EntityLabel { get; }

        /// <summary>Number of records referencing each of the given lookup rows.</summary>
        protected abstract Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids);

        protected override IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, LookupSearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name;
                    query = query.Where(x => x.Name.Contains(name));
                }

                if (search.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == search.IsActive.Value);
                }
            }

            return query;
        }

        protected override string? DefaultSortBy => "Id desc";

        public override async Task<PageResult<TResponse>> GetAllAsync(LookupSearchObject? search = null)
        {
            var result = await base.GetAllAsync(search);

            var ids = result.Items.Select(i => i.Id).ToList();
            if (ids.Count > 0)
            {
                var usages = await CountUsagesAsync(ids);
                foreach (var item in result.Items)
                {
                    ApplyUsage(item, usages.TryGetValue(item.Id, out var count) ? count : 0);
                }
            }

            return result;
        }

        public override async Task<TResponse> GetByIdAsync(int id)
        {
            var response = await base.GetByIdAsync(id);
            var usages = await CountUsagesAsync(new[] { id });
            ApplyUsage(response, usages.TryGetValue(id, out var count) ? count : 0);
            return response;
        }

        public override async Task<TResponse> InsertAsync(TInsertRequest request)
        {
            await EnsureNameIsFreeAsync(request.Name, excludeId: null);
            return await base.InsertAsync(request);
        }

        public override async Task<TResponse> UpdateAsync(int id, TUpdateRequest request)
        {
            await EnsureNameIsFreeAsync(request.Name, excludeId: id);
            return await base.UpdateAsync(id, request);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"{typeof(TEntity).Name} with id {id} not found.");
            }

            var usages = await CountUsagesAsync(new[] { id });
            if (usages.TryGetValue(id, out var count) && count > 0)
            {
                throw new ClientException(
                    $"'{entity.Name}' is still used by {count} record(s), so it cannot be deleted. " +
                    $"Reassign those records to a different {EntityLabel} first, or mark this one as inactive.");
            }

            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        private async Task EnsureNameIsFreeAsync(string name, int? excludeId)
        {
            var trimmed = (name ?? string.Empty).Trim();
            var taken = await _dbContext.Set<TEntity>()
                .AnyAsync(x => x.Name == trimmed && (excludeId == null || x.Id != excludeId));

            if (taken)
            {
                throw new ClientException($"A {EntityLabel} named '{trimmed}' already exists.");
            }
        }

        private void ApplyUsage(TResponse response, int count)
        {
            response.InUseCount = count;
            response.CanDelete = count == 0;
            response.DeleteBlockedReason = count == 0
                ? null
                : $"Used by {count} record(s).";
        }
    }
}
