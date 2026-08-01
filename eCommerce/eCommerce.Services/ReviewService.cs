using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using eCommerce.Model.Enums;

namespace eCommerce.Services
{
    /// <summary>
    /// Customer movie reviews. Reviews are always created for the current authenticated user
    /// (a client-supplied user id is ignored) and a user may review a given movie only once.
    /// Reads are public so movie pages can display ratings; ownership is enforced on writes.
    /// </summary>
    public class ReviewService : BaseReadService<Review, ReviewResponse, ReviewSearchObject>, IReviewService
    {
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IValidator<ReviewInsertRequest> _insertValidator;
        private readonly IValidator<ReviewUpdateRequest> _updateValidator;
        private readonly IAnalyticsNotifier _analyticsNotifier;

        public ReviewService(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IAuthenticatedUserAccessor userAccessor,
            IValidator<ReviewInsertRequest> insertValidator,
            IValidator<ReviewUpdateRequest> updateValidator,
            IAnalyticsNotifier analyticsNotifier)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
            _insertValidator = insertValidator;
            _updateValidator = updateValidator;
            _analyticsNotifier = analyticsNotifier;
        }

        protected override IEnumerable<Review> ApplyFilters(IEnumerable<Review> query, ReviewSearchObject? search)
        {
            // Filtering handled in GetAllAsync against the database.
            return query;
        }

        public override async Task<PageResult<ReviewResponse>> GetAllAsync(ReviewSearchObject? search = null)
        {
            search ??= new ReviewSearchObject();
            PagingLimits.Normalize(search);

            IQueryable<Review> query = _dbContext.Reviews
                .AsNoTracking()
                .Include(r => r.Movie)
                .Include(r => r.User);

            if (search.MovieId.HasValue)
            {
                query = query.Where(r => r.MovieId == search.MovieId.Value);
            }
            if (search.UserId.HasValue)
            {
                query = query.Where(r => r.UserId == search.UserId.Value);
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderByDescending(r => r.CreatedAt)
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var entities = await query.ToListAsync();

            return new PageResult<ReviewResponse>
            {
                Items = entities.Select(MapToResponse).ToList(),
                TotalCount = totalCount
            };
        }

        public override async Task<ReviewResponse> GetByIdAsync(int id)
        {
            var review = await _dbContext.Reviews
                .AsNoTracking()
                .Include(r => r.Movie)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Review with id {id} not found.");

            return MapToResponse(review);
        }

        public async Task<ReviewResponse> InsertAsync(ReviewInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var movieExists = await _dbContext.Movies.AnyAsync(m => m.Id == request.MovieId);
            if (!movieExists)
            {
                throw new ClientException($"Movie {request.MovieId} was not found.");
            }

            var alreadyReviewed = await _dbContext.Reviews
                .AnyAsync(r => r.UserId == userId && r.MovieId == request.MovieId);
            if (alreadyReviewed)
            {
                throw new ClientException("You have already reviewed this movie.");
            }

            if (!await UserCanReviewMovieAsync(userId, request.MovieId))
            {
                throw new ClientException(
                    "You can only review a movie after attending a paid or confirmed screening.");
            }

            var review = new Review
            {
                UserId = userId,
                MovieId = request.MovieId,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Reviews.Add(review);
            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(review.Id);
        }

        public async Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var review = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Review with id {id} not found.");

            if (review.UserId != userId)
            {
                throw new ClientException("You can only edit your own review.");
            }

            review.Rating = request.Rating;
            review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
            review.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(review.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var review = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Review with id {id} not found.");

            // Users may remove their own review; administrators may remove any review.
            if (review.UserId != userId && !_userAccessor.IsInRole(RoleNames.Admin))
            {
                throw new ClientException("You can only delete your own review.");
            }

            _dbContext.Reviews.Remove(review);
            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        }

        public async Task<List<ReviewEligibilityResponse>> GetMyEligibilityAsync()
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var now = DateTime.UtcNow;

            var attendedMovies = await _dbContext.Reservations
                .AsNoTracking()
                .Where(r =>
                    r.UserId == userId
                    && (r.Status == ReservationStatus.Paid || r.Status == ReservationStatus.Confirmed)
                    && r.Screening.EndTime <= now)
                .Select(r => new
                {
                    r.Screening.MovieId,
                    MovieTitle = r.Screening.Movie != null ? r.Screening.Movie.Title : string.Empty
                })
                .Distinct()
                .ToListAsync();

            var reviewedByMovie = await _dbContext.Reviews
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.MovieId, r => r.Id);

            return attendedMovies
                .Select(m =>
                {
                    reviewedByMovie.TryGetValue(m.MovieId, out var reviewId);
                    var hasReview = reviewedByMovie.ContainsKey(m.MovieId);
                    return new ReviewEligibilityResponse
                    {
                        MovieId = m.MovieId,
                        MovieTitle = m.MovieTitle,
                        HasReview = hasReview,
                        ExistingReviewId = hasReview ? reviewId : null,
                        CanReview = !hasReview
                    };
                })
                .OrderBy(e => e.MovieTitle)
                .ToList();
        }

        private async Task<bool> UserCanReviewMovieAsync(int userId, int movieId)
        {
            var now = DateTime.UtcNow;
            return await _dbContext.Reservations
                .AnyAsync(r =>
                    r.UserId == userId
                    && (r.Status == ReservationStatus.Paid || r.Status == ReservationStatus.Confirmed)
                    && r.Screening.MovieId == movieId
                    && r.Screening.EndTime <= now);
        }

        private static ReviewResponse MapToResponse(Review r)
        {
            return new ReviewResponse
            {
                Id = r.Id,
                MovieId = r.MovieId,
                MovieTitle = r.Movie?.Title ?? string.Empty,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}".Trim() : string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
