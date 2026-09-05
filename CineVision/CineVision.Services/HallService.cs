using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class HallService : BaseCRUDService<Hall, HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest>, IHallService
    {
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly IValidator<HallSeatLayoutUpdateRequest> _seatLayoutValidator;

        public HallService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<HallInsertRequest> insertValidator,
            IValidator<HallUpdateRequest> updateValidator,
            IValidator<HallSeatLayoutUpdateRequest> seatLayoutValidator,
            IAnalyticsNotifier analyticsNotifier)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _seatLayoutValidator = seatLayoutValidator;
            _analyticsNotifier = analyticsNotifier;
        }

        protected override async Task<IQueryable<Hall>> IncludeRelatedEntitiesAsync(HallSearchObject? search, IQueryable<Hall> query = null!)
        {
            // Screen type and status names are always part of the response.
            query = query
                .Include(h => h.ScreenType)
                .Include(h => h.Status);

            if (search?.IncludeSeats == true)
            {
                query = query.Include(h => h.Seats);
            }
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        protected override IQueryable<Hall> ApplyFilters(IQueryable<Hall> query, HallSearchObject? search)
        {
            if (search != null && !string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name;
                query = query.Where(h => h.Name.Contains(name));
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

            var screenType = await RequireScreenTypeAsync(request.ScreenTypeId);
            var status = await RequireStatusAsync(request.StatusId);

            var hall = new Hall
            {
                Name = request.Name,
                ScreenTypeId = screenType.Id,
                StatusId = status.Id,
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
            // Without Normalize a client can ask for every hall (or millions of rows) with no cap.
            PagingLimits.Normalize(search);

            IQueryable<Hall> query = _dbContext.Halls
                .AsNoTracking()
                .Include(h => h.ScreenType)
                .Include(h => h.Status)
                .Include(h => h.Seats);

            query = ApplyFilters(query, search);

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            // Newest first by default so a hall added a moment ago is the first row.
            query = string.IsNullOrWhiteSpace(search.SortBy)
                ? query.OrderByDescending(h => h.Id)
                : query.OrderBy(search.SortBy);

            query = query
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

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
                .Include(h => h.ScreenType)
                .Include(h => h.Status)
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
                .Include(h => h.ScreenType)
                .Include(h => h.Status)
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hall with id {id} not found.");

            var screenType = await RequireScreenTypeAsync(request.ScreenTypeId);
            var status = await RequireStatusAsync(request.StatusId);

            if (!status.AllowsProjections)
            {
                var upcoming = await _dbContext.Projections.CountAsync(s =>
                    s.HallId == id &&
                    s.CancelledAt == null &&
                    s.StartTime >= DateTime.UtcNow);

                if (upcoming > 0)
                {
                    throw new ClientException(
                        $"Hall '{hall.Name}' still has {upcoming} upcoming projection(s). " +
                        "Cancel those projections before setting the hall to a status that does not allow shows.");
                }
            }

            if (hall.ScreenTypeId != screenType.Id && await HasSoldUpcomingSeatsAsync(id))
            {
                throw new ClientException(
                    $"Hall '{hall.Name}' has upcoming projections with sold seats. The screen type cannot change " +
                    "because those tickets were bought for the current format. Cancel those projections first.");
            }

            hall.Name = request.Name;
            hall.ScreenType = screenType;
            hall.ScreenTypeId = screenType.Id;
            hall.Status = status;
            hall.StatusId = status.Id;
            hall.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return BuildResponse(hall, includeSeats: true);
        }

        public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var hall = await _dbContext.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hall with id {id} not found.");

            var projectionIds = await _dbContext.Projections
                .AsNoTracking()
                .Where(s => s.HallId == id)
                .Select(s => s.Id)
                .ToListAsync();

            var graph = await BookingGraphCascade.CountForProjectionIdsAsync(_dbContext, projectionIds);
            var history = await BookingGraphCascade.CountBookingHistoryAsync(_dbContext, projectionIds);
            var seatCount = await _dbContext.Seats.CountAsync(s => s.HallId == id);

            return BookingGraphCascade.BuildImpact(
                hall.Id,
                hall.Name,
                history,
                ("Projections", graph.ProjectionCount),
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
                var projectionIds = await _dbContext.Projections
                    .Where(s => s.HallId == id)
                    .Select(s => s.Id)
                    .ToListAsync();

                await BookingGraphCascade.RemoveProjectionsAsync(
                    _dbContext,
                    projectionIds,
                    $"Hall '{hall.Name}'");

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
            await _seatLayoutValidator.ValidateAndThrowAsync(request);

            var hall = await _dbContext.Halls
                .Include(h => h.Seats)
                .FirstOrDefaultAsync(h => h.Id == hallId)
                ?? throw new KeyNotFoundException($"Hall with id {hallId} not found.");

            var seats = hall.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).ToList();
            var byId = seats.ToDictionary(s => s.Id);
            var rows = seats.GroupBy(s => s.RowLabel).ToDictionary(g => g.Key, g => g.OrderBy(s => s.SeatNumber).ToList());

            if (SeatLayoutWouldChange(seats, rows, request) && await HasSoldUpcomingSeatsAsync(hallId))
            {
                throw new ClientException(
                    "This hall has upcoming projections with sold seats. Regular/couple layout cannot change " +
                    "because those tickets still point at the current seats. Cancel those projections first.");
            }

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

                if (item.SeatType != (int)SeatType.Regular && item.SeatType != (int)SeatType.Couple)
                {
                    // Defensive: FluentValidation already rejects invalid types.
                    throw new ClientException($"Invalid seat type for seat {seat.RowLabel}{seat.SeatNumber}. Use Regular or Couple only.");
                }

                // Layouts saved before VIP was retired still carry it; fold them back to Regular.
                if (item.SeatType == (int)SeatType.VIP)
                {
                    item.SeatType = (int)SeatType.Regular;
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

        /// <summary>
        /// True while a still-scheduled projection in this hall holds seats that were not released,
        /// i.e. there are live tickets whose meaning a hall change would rewrite.
        /// </summary>
        private async Task<bool> HasSoldUpcomingSeatsAsync(int hallId)
        {
            var now = DateTime.UtcNow;
            return await _dbContext.ReservationSeats.AnyAsync(rs =>
                rs.ReleasedAt == null &&
                rs.Projection.HallId == hallId &&
                rs.Projection.CancelledAt == null &&
                rs.Projection.StartTime >= now);
        }

        private static bool SeatLayoutWouldChange(
            List<Seat> seats,
            Dictionary<string, List<Seat>> rows,
            HallSeatLayoutUpdateRequest request)
        {
            var byId = seats.ToDictionary(s => s.Id);
            var requested = request.Seats.ToDictionary(x => x.SeatId, x =>
            {
                var type = x.SeatType == (int)SeatType.VIP ? (int)SeatType.Regular : x.SeatType;
                return type;
            });

            var desiredPartner = new Dictionary<int, int?>();
            foreach (var seat in seats)
            {
                desiredPartner[seat.Id] = null;
            }

            foreach (var item in request.Seats.Where(x =>
                         (x.SeatType == (int)SeatType.VIP ? (int)SeatType.Regular : x.SeatType) == (int)SeatType.Couple))
            {
                if (!byId.TryGetValue(item.SeatId, out var seat))
                {
                    continue;
                }

                if (!rows.TryGetValue(seat.RowLabel, out var rowSeats))
                {
                    continue;
                }

                var index = rowSeats.FindIndex(s => s.Id == seat.Id);
                if (index >= 0 && index < rowSeats.Count - 1)
                {
                    desiredPartner[seat.Id] = rowSeats[index + 1].Id;
                }
            }

            foreach (var seat in seats)
            {
                var desiredType = requested.TryGetValue(seat.Id, out var type)
                    ? type
                    : (int)seat.SeatType;
                if (desiredPartner.TryGetValue(seat.Id, out var partner) && partner.HasValue)
                {
                    desiredType = (int)SeatType.Couple;
                }
                else if (desiredPartner.ContainsValue(seat.Id))
                {
                    desiredType = (int)SeatType.Regular;
                }

                if ((int)seat.SeatType != desiredType || seat.PartnerSeatId != desiredPartner[seat.Id])
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<ScreenType> RequireScreenTypeAsync(int screenTypeId)
        {
            return await _dbContext.ScreenTypes.FirstOrDefaultAsync(s => s.Id == screenTypeId)
                ?? throw new ClientException("The selected screen type no longer exists. Refresh and pick another one.");
        }

        private async Task<HallStatus> RequireStatusAsync(int statusId)
        {
            return await _dbContext.HallStatuses.FirstOrDefaultAsync(s => s.Id == statusId)
                ?? throw new ClientException("The selected hall status no longer exists. Refresh and pick another one.");
        }

        private HallResponse BuildResponse(Hall hall, bool includeSeats)
        {
            var response = _mapper.Map<HallResponse>(hall);
            response.ScreenTypeName = hall.ScreenType?.Name ?? string.Empty;
            response.StatusName = hall.Status?.Name ?? string.Empty;
            response.AllowsProjections = hall.Status?.AllowsProjections ?? false;
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
