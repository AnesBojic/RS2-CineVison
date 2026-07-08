using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class HallService : BaseCRUDService<Hall, HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest>, IHallService
    {
        public HallService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<HallInsertRequest> insertValidator, IValidator<HallUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override async Task<IQueryable<Hall>> IncludeRelatedEntitiesAsync(HallSearchObject? search, IQueryable<Hall> query = null!)
        {
            if (search?.IncludeSeats == true)
            {
                query = query.Include(h => h.Seats);
            }
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        protected override IEnumerable<Hall> ApplyFilters(IEnumerable<Hall> query, HallSearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(h => h.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }

        public override async Task<HallResponse> InsertAsync(HallInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var status = (HallStatus)request.Status;
            var hall = new Hall
            {
                Name = request.Name,
                Description = request.Description,
                ScreenType = (ScreenType)request.ScreenType,
                Status = status,
                // Keep the legacy IsActive flag in sync with the richer status.
                IsActive = status == HallStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            if (request.RowsCount > 0 && request.SeatsPerRow > 0)
            {
                for (int r = 0; r < request.RowsCount; r++)
                {
                    var rowLabel = ToRowLabel(r);
                    for (int n = 1; n <= request.SeatsPerRow; n++)
                    {
                        hall.Seats.Add(new Seat
                        {
                            RowLabel = rowLabel,
                            SeatNumber = n,
                            SeatType = SeatType.Regular,
                            IsActive = true
                        });
                    }
                }
            }

            _dbContext.Halls.Add(hall);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(hall.Id);
        }

        public override async Task<PageResult<HallResponse>> GetAllAsync(HallSearchObject? search = null)
        {
            search ??= new HallSearchObject();

            IQueryable<Hall> query = _dbContext.Halls
                .AsNoTracking()
                .Include(h => h.Seats);

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name;
                query = query.Where(h => h.Name.Contains(name));
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderBy(h => h.Name);

            if (search.Page.HasValue && search.PageSize.HasValue)
            {
                query = query.Skip((search.Page.Value - 1) * search.PageSize.Value).Take(search.PageSize.Value);
            }
            else if (search.PageSize.HasValue)
            {
                query = query.Take(search.PageSize.Value);
            }

            var halls = await query.ToListAsync();

            var items = halls
                .Select(h => BuildResponse(h, includeSeats: search.IncludeSeats == true))
                .ToList();

            return new PageResult<HallResponse> { Items = items, TotalCount = totalCount };
        }

        public override async Task<HallResponse> GetByIdAsync(int id)
        {
            var hall = await _dbContext.Halls
                .AsNoTracking()
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hall == null)
            {
                throw new KeyNotFoundException($"Hall with id {id} not found.");
            }

            return BuildResponse(hall, includeSeats: true);
        }

        public override async Task<HallResponse> UpdateAsync(int id, HallUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var hall = await _dbContext.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hall with id {id} not found.");

            var status = (HallStatus)request.Status;
            hall.Name = request.Name;
            hall.Description = request.Description;
            hall.ScreenType = (ScreenType)request.ScreenType;
            hall.Status = status;
            hall.IsActive = status == HallStatus.Active;
            hall.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return BuildResponse(hall, includeSeats: true);
        }

        private HallResponse BuildResponse(Hall hall, bool includeSeats)
        {
            var response = _mapper.Map<HallResponse>(hall);
            response.ScreenTypeName = ScreenTypeDisplayName(hall.ScreenType);
            response.StatusName = hall.Status.ToString();
            response.SeatCount = hall.Seats.Count;
            response.Capacity = hall.Seats.Count;
            response.Seats = includeSeats
                ? hall.Seats
                    .OrderBy(s => s.RowLabel)
                    .ThenBy(s => s.SeatNumber)
                    .Select(s => _mapper.Map<SeatResponse>(s))
                    .ToList()
                : new List<SeatResponse>();
            return response;
        }

        private static string ScreenTypeDisplayName(ScreenType screenType) => screenType switch
        {
            ScreenType.ThreeD => "3D",
            _ => screenType.ToString()
        };

        private static string ToRowLabel(int index)
        {
            // 0 -> A, 25 -> Z, 26 -> AA, ...
            var label = string.Empty;
            index += 1;
            while (index > 0)
            {
                index--;
                label = (char)('A' + (index % 26)) + label;
                index /= 26;
            }
            return label;
        }
    }
}
