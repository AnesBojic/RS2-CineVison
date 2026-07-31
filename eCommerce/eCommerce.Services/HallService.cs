using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class HallService : BaseCRUDService<Hall, HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest>, IHallService
    {
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly string? _stripeSecretKey;
        private readonly ILogger<HallService> _logger;

        public HallService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<HallInsertRequest> insertValidator,
            IValidator<HallUpdateRequest> updateValidator,
            IAnalyticsNotifier analyticsNotifier,
            IConfiguration configuration,
            ILogger<HallService> logger)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _analyticsNotifier = analyticsNotifier;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
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

        public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var hall = await _dbContext.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hall with id {id} not found.");

            var screeningIds = await _dbContext.Screenings
                .AsNoTracking()
                .Where(s => s.HallId == id)
                .Select(s => s.Id)
                .ToListAsync();

            var graph = await BookingGraphCascade.CountForScreeningIdsAsync(_dbContext, screeningIds);
            var seatCount = await _dbContext.Seats.CountAsync(s => s.HallId == id);

            return BookingGraphCascade.BuildImpact(
                hall.Id,
                hall.Name,
                ("Projections", graph.ScreeningCount),
                ("Reservations", graph.ReservationCount),
                ("Reserved seats", graph.ReservationSeatCount),
                ("Seats", seatCount));
        }

        public override async Task DeleteAsync(int id)
        {
            var hall = await _dbContext.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hall with id {id} not found.");

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var screeningIds = await _dbContext.Screenings
                    .Where(s => s.HallId == id)
                    .Select(s => s.Id)
                    .ToListAsync();

                await BookingGraphCascade.RemoveScreeningsAsync(
                    _dbContext,
                    screeningIds,
                    paymentIntentId => StripeRefundHelper.TryRefundAsync(_stripeSecretKey, paymentIntentId, _logger));

                // PartnerSeat is Restrict — clear links before seats cascade with the hall.
                foreach (var seat in hall.Seats)
                {
                    seat.PartnerSeatId = null;
                }

                _dbContext.Halls.Remove(hall);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        }

        public async Task<HallResponse> UpdateSeatLayoutAsync(int hallId, HallSeatLayoutUpdateRequest request)
        {
            if (request.Seats == null || request.Seats.Count == 0)
            {
                throw new ClientException("No seat layout was provided.");
            }

            var hall = await _dbContext.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == hallId)
                ?? throw new KeyNotFoundException($"Hall with id {hallId} not found.");

            var seats = hall.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).ToList();
            var byId = seats.ToDictionary(s => s.Id);
            var rows = seats.GroupBy(s => s.RowLabel).ToDictionary(g => g.Key, g => g.OrderBy(s => s.SeatNumber).ToList());

            // Clear existing couple links and reactivate partner seats.
            foreach (var seat in seats)
            {
                seat.PartnerSeatId = null;
                seat.IsActive = true;
            }

            var couplePrimaryIds = request.Seats
                .Where(x => x.SeatType == (int)SeatType.Couple)
                .Select(x => x.SeatId)
                .ToHashSet();

            foreach (var item in request.Seats)
            {
                if (!byId.TryGetValue(item.SeatId, out var seat))
                {
                    throw new ClientException($"Seat {item.SeatId} does not belong to this hall.");
                }

                if (item.SeatType != 0 && item.SeatType != (int)SeatType.Couple)
                {
                    throw new ClientException($"Invalid seat type for seat {seat.RowLabel}{seat.SeatNumber}. Use Regular or Couple only.");
                }

                if (item.SeatType == 1)
                {
                    item.SeatType = 0;
                }

                seat.SeatType = (SeatType)item.SeatType;
                seat.PartnerSeatId = null;
                seat.IsActive = true;
            }

            foreach (var item in request.Seats.Where(x => x.SeatType == (int)SeatType.Couple))
            {
                if (!byId.TryGetValue(item.SeatId, out var seat))
                {
                    continue;
                }

                if (!rows.TryGetValue(seat.RowLabel, out var rowSeats))
                {
                    throw new ClientException($"Row {seat.RowLabel} was not found.");
                }

                var index = rowSeats.FindIndex(s => s.Id == seat.Id);
                if (index < 0 || index >= rowSeats.Count - 1)
                {
                    throw new ClientException(
                        $"Seat {seat.RowLabel}{seat.SeatNumber} cannot be a couple seat — there is no seat to the right.");
                }

                var partner = rowSeats[index + 1];
                if (couplePrimaryIds.Contains(partner.Id))
                {
                    throw new ClientException(
                        $"Seat {partner.RowLabel}{partner.SeatNumber} is already marked as a couple seat.");
                }

                seat.PartnerSeatId = partner.Id;
                partner.IsActive = false;
                partner.SeatType = SeatType.Regular;
                partner.PartnerSeatId = null;
            }

            hall.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return BuildResponse(hall, includeSeats: true);
        }

        private HallResponse BuildResponse(Hall hall, bool includeSeats)
        {
            var response = _mapper.Map<HallResponse>(hall);
            response.ScreenTypeName = ScreenTypeDisplayName(hall.ScreenType);
            response.StatusName = hall.Status.ToString();
            response.SeatCount = hall.Seats.Count(s => s.IsActive);
            response.Capacity = hall.Seats.Count(s => s.IsActive);
            var rowGroups = hall.Seats.GroupBy(s => s.RowLabel).ToList();
            response.RowCount = rowGroups.Count;
            response.SeatsPerRow = rowGroups.Count > 0 ? rowGroups.Max(g => g.Count()) : 0;
            response.Seats = includeSeats
                ? hall.Seats
                    .OrderBy(s => s.RowLabel)
                    .ThenBy(s => s.SeatNumber)
                    .Select(MapSeatResponse)
                    .ToList()
                : new List<SeatResponse>();
            return response;
        }

        private static SeatResponse MapSeatResponse(Seat s)
        {
            return new SeatResponse
            {
                Id = s.Id,
                HallId = s.HallId,
                RowLabel = s.RowLabel,
                SeatNumber = s.SeatNumber,
                SeatType = (int)s.SeatType,
                PartnerSeatId = s.PartnerSeatId,
                SpotsOccupied = s.SeatType == SeatType.Couple ? 2 : 1,
                IsActive = s.IsActive,
            };
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
