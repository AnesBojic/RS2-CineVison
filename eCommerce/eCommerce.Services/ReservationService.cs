using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace eCommerce.Services
{
    public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
    {
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(ECommerceDbContext dbContext, IMapper mapper, IAuthenticatedUserAccessor userAccessor, IConfiguration configuration, IEmailService emailService, ILogger<ReservationService> logger)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        protected override IEnumerable<Reservation> ApplyFilters(IEnumerable<Reservation> query, ReservationSearchObject? search)
        {
            // Filtering handled in GetAllAsync against the database.
            return query;
        }

        public override async Task<PageResult<ReservationResponse>> GetAllAsync(ReservationSearchObject? search = null)
        {
            search ??= new ReservationSearchObject();

            var userId = _userAccessor.GetUserId();
            if (!userId.HasValue)
            {
                return new PageResult<ReservationResponse> { Items = new List<ReservationResponse>(), TotalCount = 0 };
            }

            IQueryable<Reservation> query = _dbContext.Reservations
                .AsNoTracking()
                .Include(r => r.Screening).ThenInclude(s => s.Movie)
                .Include(r => r.Screening).ThenInclude(s => s.Hall)
                .Include(r => r.ReservationSeats).ThenInclude(rs => rs.Seat)
                .Where(r => r.UserId == userId.Value);

            if (search.Status.HasValue)
            {
                query = query.Where(r => (int)r.Status == search.Status.Value);
            }
            if (search.ScreeningId.HasValue)
            {
                query = query.Where(r => r.ScreeningId == search.ScreeningId.Value);
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderByDescending(r => r.ReservationDate);

            if (search.Page.HasValue && search.PageSize.HasValue)
            {
                query = query.Skip((search.Page.Value - 1) * search.PageSize.Value).Take(search.PageSize.Value);
            }
            else if (search.PageSize.HasValue)
            {
                query = query.Take(search.PageSize.Value);
            }

            var entities = await query.ToListAsync();

            return new PageResult<ReservationResponse>
            {
                Items = entities.Select(MapToResponse).ToList(),
                TotalCount = totalCount
            };
        }

        public override async Task<ReservationResponse> GetByIdAsync(int id)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            var reservation = await _dbContext.Reservations
                .AsNoTracking()
                .Include(r => r.Screening).ThenInclude(s => s.Movie)
                .Include(r => r.Screening).ThenInclude(s => s.Hall)
                .Include(r => r.ReservationSeats).ThenInclude(rs => rs.Seat)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            return MapToResponse(reservation);
        }

        public async Task<ReservationResponse> CreateReservationAsync(ReservationCreateRequest request)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var seatIds = (request.SeatIds ?? new List<int>()).Distinct().ToList();
            if (seatIds.Count == 0)
            {
                throw new ClinetException("No seats were selected.");
            }

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var screening = await _dbContext.Screenings
                    .Include(s => s.Hall).ThenInclude(h => h.Seats)
                    .FirstOrDefaultAsync(s => s.Id == request.ScreeningId)
                    ?? throw new ClinetException($"Screening {request.ScreeningId} was not found.");

                if (!screening.IsActive)
                {
                    throw new ClinetException("This screening is not available for booking.");
                }

                if (screening.StartTime <= DateTime.UtcNow)
                {
                    throw new ClinetException("This screening has already started.");
                }

                var hallSeats = screening.Hall.Seats.ToDictionary(s => s.Id);
                var expandedSeatIds = new HashSet<int>();
                foreach (var seatId in seatIds)
                {
                    if (!hallSeats.TryGetValue(seatId, out var seat) || !seat.IsActive)
                    {
                        throw new ClinetException($"Seat {seatId} does not belong to this screening's hall or is not available.");
                    }

                    expandedSeatIds.Add(seatId);
                    if (seat.SeatType == SeatType.Couple)
                    {
                        if (!seat.PartnerSeatId.HasValue)
                        {
                            throw new ClinetException($"Couple seat {seat.RowLabel}{seat.SeatNumber} is not configured correctly.");
                        }

                        expandedSeatIds.Add(seat.PartnerSeatId.Value);
                    }
                }

                var expandedList = expandedSeatIds.ToList();

                var alreadyTaken = await _dbContext.ReservationSeats
                    .Where(rs => rs.ScreeningId == screening.Id && expandedList.Contains(rs.SeatId))
                    .AnyAsync();

                if (alreadyTaken)
                {
                    throw new ClinetException("One or more of the selected seats are already reserved.");
                }

                var total = screening.BasePrice * expandedList.Count;

                var reservation = new Reservation
                {
                    UserId = userId,
                    ScreeningId = screening.Id,
                    ReservationDate = DateTime.UtcNow,
                    ReservationNumber = $"R-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId}",
                    Status = string.IsNullOrWhiteSpace(request.PaymentIntentId)
                        ? ReservationStatus.Confirmed
                        : ReservationStatus.Paid,
                    TotalAmount = total,
                    CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
                    CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
                    PaymentTransactionId = request.PaymentIntentId,
                    PaymentDate = string.IsNullOrWhiteSpace(request.PaymentIntentId) ? (DateTime?)null : DateTime.UtcNow
                };

                foreach (var seatId in expandedList)
                {
                    reservation.ReservationSeats.Add(new ReservationSeat
                    {
                        SeatId = seatId,
                        ScreeningId = screening.Id,
                        Price = screening.BasePrice
                    });
                }

                _dbContext.Reservations.Add(reservation);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                var response = await GetByIdAsync(reservation.Id);

                // Queue a confirmation email; a queue/broker outage must never fail the reservation.
                await SendConfirmationEmailAsync(reservation, response);

                return response;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task SendConfirmationEmailAsync(Reservation reservation, ReservationResponse response)
        {
            try
            {
                var to = reservation.CustomerEmail;
                if (string.IsNullOrWhiteSpace(to))
                {
                    to = await _dbContext.Users
                        .Where(u => u.Id == reservation.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();
                }

                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogWarning("Reservation {ReservationId} has no email address; confirmation not sent.", reservation.Id);
                    return;
                }

                var seats = string.Join(", ", response.Seats.Select(s => $"{s.RowLabel}{s.SeatNumber}"));

                var body =
                    $"Your booking is confirmed!\n\n" +
                    $"Reservation: {response.ReservationNumber}\n" +
                    $"Movie: {response.MovieTitle}\n" +
                    $"Hall: {response.HallName}\n" +
                    $"Start: {response.ScreeningStartTime:yyyy-MM-dd HH:mm} UTC\n" +
                    $"Seats: {seats}\n" +
                    $"Total: {response.TotalAmount:0.00}\n\n" +
                    $"Thank you for booking with CineVision.";

                await _emailService.QueueEmailAsync(new EmailMessage
                {
                    To = to,
                    Subject = $"CineVision booking confirmation {response.ReservationNumber}",
                    Body = body,
                    IsHtml = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue confirmation email for reservation {ReservationId}.", reservation.Id);
            }
        }

        public async Task<ReservationResponse> CancelAsync(int id)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var reservation = await _dbContext.Reservations
                .Include(r => r.ReservationSeats)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                throw new ClinetException("This reservation is already cancelled.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            // Free the seats so they become available again for the screening.
            _dbContext.ReservationSeats.RemoveRange(reservation.ReservationSeats);
            await _dbContext.SaveChangesAsync();

            return await GetByIdAsync(reservation.Id);
        }

        public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
        {
            var seatIds = (request.SeatIds ?? new List<int>()).Distinct().ToList();
            if (seatIds.Count == 0)
            {
                throw new ClinetException("No seats were selected.");
            }

            var screening = await _dbContext.Screenings.FindAsync(request.ScreeningId)
                ?? throw new ClinetException($"Screening {request.ScreeningId} was not found.");

            var total = screening.BasePrice * seatIds.Count;

            var secretKey = _configuration["Stripe:SecretKey"]
                            ?? throw new InvalidOperationException("Stripe secret key is not configured.");

            StripeConfiguration.ApiKey = secretKey;
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(total * 100), // amount in cents
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return new PaymentIntentResponse
            {
                ClientSecret = intent.ClientSecret,
                PublishableKey = _configuration["Stripe:PublishableKey"]
                                 ?? throw new InvalidOperationException("Stripe publishable key is not configured.")
            };
        }

        private ReservationResponse MapToResponse(Reservation r)
        {
            return new ReservationResponse
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                ReservationDate = r.ReservationDate,
                Status = (int)r.Status,
                StatusName = r.Status.ToString(),
                TotalAmount = r.TotalAmount,
                UserId = r.UserId,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                ScreeningId = r.ScreeningId,
                MovieTitle = r.Screening?.Movie?.Title ?? string.Empty,
                HallName = r.Screening?.Hall?.Name ?? string.Empty,
                ScreeningStartTime = r.Screening?.StartTime ?? default,
                PaymentTransactionId = r.PaymentTransactionId,
                PaymentDate = r.PaymentDate,
                Seats = r.ReservationSeats
                    .OrderBy(rs => rs.Seat != null ? rs.Seat.RowLabel : string.Empty)
                    .ThenBy(rs => rs.Seat != null ? rs.Seat.SeatNumber : 0)
                    .Select(rs => new ReservationSeatResponse
                    {
                        Id = rs.Id,
                        SeatId = rs.SeatId,
                        RowLabel = rs.Seat?.RowLabel ?? string.Empty,
                        SeatNumber = rs.Seat?.SeatNumber ?? 0,
                        SeatType = (int)(rs.Seat?.SeatType ?? SeatType.Regular),
                        Price = rs.Price
                    })
                    .ToList()
            };
        }
    }
}
