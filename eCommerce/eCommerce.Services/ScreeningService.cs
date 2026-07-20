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

namespace eCommerce.Services
{
    public class ScreeningService : BaseCRUDService<Screening, ScreeningResponse, ScreeningSearchObject, ScreeningInsertRequest, ScreeningUpdateRequest>, IScreeningService
    {
        public ScreeningService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<ScreeningInsertRequest> insertValidator, IValidator<ScreeningUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Screening> ApplyFilters(IEnumerable<Screening> query, ScreeningSearchObject? search)
        {
            // Filtering is handled in GetAllAsync against the database query.
            return query;
        }

        public override async Task<PageResult<ScreeningResponse>> GetAllAsync(ScreeningSearchObject? search = null)
        {
            search ??= new ScreeningSearchObject();

            var includeSeatStats = search.IncludeSeatStats == true;

            IQueryable<Screening> query = _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Movie);

            if (includeSeatStats)
            {
                query = query
                    .Include(s => s.Hall).ThenInclude(h => h.Seats)
                    .Include(s => s.ReservationSeats);
            }
            else
            {
                query = query.Include(s => s.Hall);
            }

            if (search.MovieId.HasValue)
            {
                query = query.Where(s => s.MovieId == search.MovieId.Value);
            }
            if (search.HallId.HasValue)
            {
                query = query.Where(s => s.HallId == search.HallId.Value);
            }
            if (search.FromDate.HasValue)
            {
                query = query.Where(s => s.StartTime >= search.FromDate.Value);
            }
            if (search.ToDate.HasValue)
            {
                query = query.Where(s => s.StartTime <= search.ToDate.Value);
            }
            if (search.OnlyUpcoming == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(s => s.StartTime >= now);
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderBy(s => s.StartTime);

            if (search.Page.HasValue && search.PageSize.HasValue)
            {
                query = query.Skip((search.Page.Value - 1) * search.PageSize.Value).Take(search.PageSize.Value);
            }
            else if (search.PageSize.HasValue)
            {
                query = query.Take(search.PageSize.Value);
            }

            var entities = await query.ToListAsync();
            var items = entities.Select(s => MapToResponse(
                s,
                search.IncludeMovie == true,
                search.IncludeHall == true,
                includeSeatStats)).ToList();

            return new PageResult<ScreeningResponse>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public override async Task<ScreeningResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Movie).ThenInclude(m => m.Genre)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.ReservationSeats)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

            return MapToResponse(entity, includeMovie: true, includeHall: true, includeSeatStats: true);
        }

        public override async Task<ScreeningResponse> InsertAsync(ScreeningInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClinetException($"Movie {request.MovieId} was not found.");

            var hall = await _dbContext.Halls.FindAsync(request.HallId)
                ?? throw new ClinetException($"Hall {request.HallId} was not found.");

            if (hall.Status != HallStatus.Active)
            {
                throw new ClinetException($"Hall '{hall.Name}' is not available (status: {hall.Status}). Projections can only be scheduled in active halls.");
            }

            var entity = new Screening
            {
                MovieId = request.MovieId,
                HallId = request.HallId,
                StartTime = request.StartTime,
                EndTime = request.StartTime.AddMinutes(movie.DurationMinutes),
                BasePrice = request.BasePrice,
                Language = request.Language,
                HasSubtitles = request.HasSubtitles,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Screenings.Add(entity);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
        }

        public override async Task<ScreeningResponse> UpdateAsync(int id, ScreeningUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var entity = await _dbContext.Screenings.FindAsync(id)
                ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClinetException($"Movie {request.MovieId} was not found.");

            var hall = await _dbContext.Halls.FindAsync(request.HallId)
                ?? throw new ClinetException($"Hall {request.HallId} was not found.");

            if (hall.Status != HallStatus.Active)
            {
                throw new ClinetException($"Hall '{hall.Name}' is not available (status: {hall.Status}). Projections can only be scheduled in active halls.");
            }

            entity.MovieId = request.MovieId;
            entity.HallId = request.HallId;
            entity.StartTime = request.StartTime;
            entity.EndTime = request.StartTime.AddMinutes(movie.DurationMinutes);
            entity.BasePrice = request.BasePrice;
            entity.Language = request.Language;
            entity.HasSubtitles = request.HasSubtitles;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
        }

        public async Task<List<ScreeningSeatResponse>> GetSeatsAsync(int screeningId)
        {
            var screening = await _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.Id == screeningId)
                ?? throw new KeyNotFoundException($"Screening with id {screeningId} not found.");

            var takenSeatIds = await _dbContext.ReservationSeats
                .Where(rs => rs.ScreeningId == screeningId)
                .Select(rs => rs.SeatId)
                .ToListAsync();

            var taken = new HashSet<int>(takenSeatIds);
            var seatsById = screening.Hall.Seats.ToDictionary(s => s.Id);

            foreach (var takenId in takenSeatIds.ToList())
            {
                if (seatsById.TryGetValue(takenId, out var takenSeat) && takenSeat.PartnerSeatId.HasValue)
                {
                    taken.Add(takenSeat.PartnerSeatId.Value);
                }

                foreach (var seat in screening.Hall.Seats)
                {
                    if (seat.PartnerSeatId == takenId)
                    {
                        taken.Add(seat.Id);
                    }
                }
            }

            return screening.Hall.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .Select(s =>
                {
                    var spots = s.SeatType == SeatType.Couple ? 2 : 1;
                    return new ScreeningSeatResponse
                    {
                        SeatId = s.Id,
                        HallId = s.HallId,
                        RowLabel = s.RowLabel,
                        SeatNumber = s.SeatNumber,
                        SeatType = (int)s.SeatType,
                        PartnerSeatId = s.PartnerSeatId,
                        SpotsOccupied = spots,
                        IsTaken = taken.Contains(s.Id) ||
                                  (s.PartnerSeatId.HasValue && taken.Contains(s.PartnerSeatId.Value)),
                        Price = screening.BasePrice * spots
                    };
                })
                .ToList();
        }

        private ScreeningResponse MapToResponse(
            Screening s,
            bool includeMovie,
            bool includeHall,
            bool includeSeatStats)
        {
            var response = _mapper.Map<ScreeningResponse>(s);
            response.MovieTitle = s.Movie?.Title ?? string.Empty;
            response.MoviePosterBase64 = s.Movie?.PosterImageBase64;
            response.HallName = s.Hall?.Name ?? string.Empty;

            if (includeSeatStats)
            {
                var totalSeats = s.Hall?.Seats.Count(x => x.IsActive) ?? 0;
                response.TotalSeats = totalSeats;
                response.AvailableSeats = Math.Max(0, totalSeats - (s.ReservationSeats?.Count ?? 0));
            }

            if (includeMovie && s.Movie != null)
            {
                response.Movie = _mapper.Map<MovieResponse>(s.Movie);
            }
            if (includeHall && s.Hall != null)
            {
                response.Hall = _mapper.Map<HallResponse>(s.Hall);
                response.Hall.SeatCount = includeSeatStats
                    ? s.Hall.Seats.Count
                    : 0;
            }

            return response;
        }
    }
}
