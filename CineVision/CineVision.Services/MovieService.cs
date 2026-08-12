using System;
using System.Collections.Generic;
using System.Linq;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CineVision.Services;

public class MovieService : BaseReadService<Movie, MovieResponse, MovieSearchObject>, IMovieService
{
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly IAnalyticsNotifier _analyticsNotifier;
    private readonly string? _stripeSecretKey;
    private readonly ILogger<MovieService> _logger;
    private readonly IValidator<MoviePosterUpdateRequest> _posterValidator;

    public MovieService(
        CineVisionDbContext dbContext,
        MapsterMapper.IMapper mapper,
        IAuthenticatedUserAccessor userAccessor,
        IAnalyticsNotifier analyticsNotifier,
        IConfiguration configuration,
        ILogger<MovieService> logger,
        IValidator<MoviePosterUpdateRequest> posterValidator)
        : base(mapper, dbContext)
    {
        _userAccessor = userAccessor;
        _analyticsNotifier = analyticsNotifier;
        _stripeSecretKey = configuration["Stripe:SecretKey"];
        _logger = logger;
        _posterValidator = posterValidator;
    }

    protected override string? DefaultSortBy => "Id desc";

    public override async Task<PageResult<MovieResponse>> GetAllAsync(MovieSearchObject? search = null)
    {
        var result = await base.GetAllAsync(search);
        if (search?.IncludePoster != true && result.Items != null)
        {
            foreach (var item in result.Items)
            {
                item.PosterImageBase64 = null;
            }
        }

        await TryRecordSearchAsync(search);
        return result;
    }

    public Task RecordSearchAsync(RecordSearchRequest request)
    {
        return TryRecordSearchAsync(new MovieSearchObject
        {
            Title = request.Title,
            GenreId = request.GenreId
        });
    }

    private async Task TryRecordSearchAsync(MovieSearchObject? search)
    {
        if (search == null)
        {
            return;
        }

        var queryText = search.Title?.Trim();
        if (string.IsNullOrWhiteSpace(queryText) && !search.GenreId.HasValue)
        {
            return;
        }

        var userId = _userAccessor.GetUserId();
        if (userId == null)
        {
            return;
        }

        // Skip near-duplicate spam (same user + query/genre within a short window).
        var since = DateTime.UtcNow.AddMinutes(-2);
        var alreadyLogged = await _dbContext.SearchHistories.AsNoTracking().AnyAsync(s =>
            s.UserId == userId.Value &&
            s.SearchedAt >= since &&
            s.GenreId == search.GenreId &&
            s.Query == (string.IsNullOrWhiteSpace(queryText) ? $"genre:{search.GenreId}" : queryText!));

        if (alreadyLogged)
        {
            return;
        }

        _dbContext.SearchHistories.Add(new SearchHistory
        {
            UserId = userId.Value,
            Query = string.IsNullOrWhiteSpace(queryText) ? $"genre:{search.GenreId}" : queryText!,
            GenreId = search.GenreId,
            SearchedAt = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // Search history must never break listing movies.
        }
    }

    protected override Task<IQueryable<Movie>> IncludeRelatedEntitiesAsync(MovieSearchObject? search, IQueryable<Movie> query = null!)
    {
        // Language and age rating names are always part of the response.
        query = query
            .Include(m => m.Language)
            .Include(m => m.AgeRating);

        if (search?.IncludeGenre == true)
        {
            query = query.Include(m => m.Genre);
        }
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IQueryable<Movie> ApplyFilters(IQueryable<Movie> query, MovieSearchObject? search)
    {
        if (search != null)
        {
            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                var title = search.Title;
                query = query.Where(m => m.Title.Contains(title));
            }
            if (!string.IsNullOrWhiteSpace(search.Description))
            {
                var description = search.Description;
                query = query.Where(m => m.Description.Contains(description));
            }
            if (search.GenreId.HasValue)
            {
                query = query.Where(m => m.GenreId == search.GenreId.Value);
            }
        }

        return query;
    }

    public async Task<MovieResponse> InsertAsync(MovieInsertRequest request)
    {
        await EnsureReferencesExistAsync(request.LanguageId, request.AgeRatingId);

        var entity = _mapper.Map<Movie>(request);
        entity.CreatedAt = DateTime.UtcNow;
        _dbContext.Movies.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        return await MapWithReferencesAsync(entity);
    }

    public async Task<MovieResponse> UpdateAsync(int id, MovieUpdateRequest request)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        await EnsureReferencesExistAsync(request.LanguageId, request.AgeRatingId);

        _mapper.Map(request, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        return await MapWithReferencesAsync(entity);
    }

    private async Task<MovieResponse> MapWithReferencesAsync(Movie entity)
    {
        var entry = _dbContext.Entry(entity);
        await entry.Reference(m => m.Genre).LoadAsync();
        await entry.Reference(m => m.Language).LoadAsync();
        await entry.Reference(m => m.AgeRating).LoadAsync();
        return _mapper.Map<MovieResponse>(entity);
    }

    /// <summary>
    /// Turns a stale reference-data selection into a readable 400 instead of a foreign key error.
    /// </summary>
    private async Task EnsureReferencesExistAsync(int? languageId, int? ageRatingId)
    {
        if (languageId.HasValue && !await _dbContext.Languages.AnyAsync(l => l.Id == languageId.Value))
        {
            throw new ClientException("The selected language no longer exists. Refresh and pick another one.");
        }

        if (ageRatingId.HasValue && !await _dbContext.AgeRatings.AnyAsync(a => a.Id == ageRatingId.Value))
        {
            throw new ClientException("The selected age rating no longer exists. Refresh and pick another one.");
        }
    }

    public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
    {
        var movie = await _dbContext.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var projectionIds = await _dbContext.Projections
            .AsNoTracking()
            .Where(s => s.MovieId == id)
            .Select(s => s.Id)
            .ToListAsync();

        var graph = await BookingGraphCascade.CountForProjectionIdsAsync(_dbContext, projectionIds);
        var reviewCount = await _dbContext.Reviews.CountAsync(r => r.MovieId == id);

        return BookingGraphCascade.BuildImpact(
            movie.Id,
            movie.Title,
            ("Projections", graph.ProjectionCount),
            ("Reservations", graph.ReservationCount),
            ("Reserved seats", graph.ReservationSeatCount),
            ("Reviews", reviewCount));
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var projectionIds = await _dbContext.Projections
                .Where(s => s.MovieId == id)
                .Select(s => s.Id)
                .ToListAsync();

            await BookingGraphCascade.RemoveProjectionsAsync(
                _dbContext,
                projectionIds,
                paymentIntentId => StripeRefundHelper.TryRefundAsync(_stripeSecretKey, paymentIntentId, _logger));

            // Reviews cascade via FK; remove root after children that Restrict.
            _dbContext.Movies.Remove(entity);
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

    public async Task RegisterViewAsync(int id)
    {
        // Cheap atomic single-row increment; no need to load the entity.
        var affected = await _dbContext.Movies
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ViewCount, m => m.ViewCount + 1));

        if (affected == 0)
        {
            throw new KeyNotFoundException($"Movie with id {id} not found.");
        }
    }

    public async Task<MovieResponse> UpdatePosterAsync(int id, MoviePosterUpdateRequest request)
    {
        await _posterValidator.ValidateAndThrowAsync(request);

        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        entity.PosterImageBase64 = request.PosterImageBase64;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public override async Task<MovieResponse> GetByIdAsync(int id)
    {
        var entity = await _dbContext.Movies
            .AsNoTracking()
            .Include(m => m.Genre)
            .Include(m => m.Language)
            .Include(m => m.AgeRating)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        return _mapper.Map<MovieResponse>(entity);
    }
}
