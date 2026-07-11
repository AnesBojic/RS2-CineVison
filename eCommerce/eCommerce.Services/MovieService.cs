using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.MovieStateMachine;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services;

public class MovieService : BaseReadService<Movie, MovieResponse, MovieSearchObject>, IMovieService
{
    protected BaseMovieState MovieState { get; }

    public MovieService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, BaseMovieState movieState)
        : base(mapper, dbContext)
    {
        MovieState = movieState;
    }

    protected override Task<IQueryable<Movie>> IncludeRelatedEntitiesAsync(MovieSearchObject? search, IQueryable<Movie> query = null!)
    {
        if (search?.IncludeGenre == true)
        {
            query = query.Include(m => m.Genre);
        }
        if (search?.IncludeAssets == true)
        {
            query = query.Include(m => m.Assets);
        }
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IEnumerable<Movie> ApplyFilters(IEnumerable<Movie> query, MovieSearchObject? search)
    {
        if (search != null)
        {
            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(m => m.Title.Contains(search.Title, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(search.Description))
            {
                query = query.Where(m => m.Description.Contains(search.Description, StringComparison.OrdinalIgnoreCase));
            }
            if (search.GenreId.HasValue)
            {
                query = query.Where(m => m.GenreId == search.GenreId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search.MovieState))
            {
                var stateFilter = search.MovieState.Trim();
                if (stateFilter.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(m => m.MovieState == nameof(ActiveMovieState));
                }
                else if (stateFilter.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(m => m.MovieState == nameof(DraftMovieState));
                }
                else
                {
                    query = query.Where(m => m.MovieState.Equals(stateFilter, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        return query;
    }

    public async Task<MovieResponse> ActivateAsync(int id)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var state = MovieState.GetMovieState(entity.MovieState);
        return await state.ActivateAsync(id);
    }

    public async Task<MovieResponse> DeactivateAsync(int id)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var state = MovieState.GetMovieState(entity.MovieState);
        return await state.DeactivateAsync(id);
    }

    public async Task<List<string>> GetAllowedActionsAsync(int id)
    {
        if (id <= 0)
        {
            var initialState = MovieState.GetMovieState(nameof(InitialMovieState));
            return initialState.GetAllowedActions();
        }

        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var state = MovieState.GetMovieState(entity.MovieState);
        return state.GetAllowedActions();
    }

    public Task<MovieResponse> InsertAsync(MovieInsertRequest request)
    {
        var state = MovieState.GetMovieState(nameof(InitialMovieState));
        return state.InsertAsync(request);
    }

    public async Task<MovieResponse> UpdateAsync(int id, MovieUpdateRequest request)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var state = MovieState.GetMovieState(entity.MovieState);
        return await state.UpdateAsync(id, request);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Movies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        if (await _dbContext.Screenings.AnyAsync(s => s.MovieId == id))
        {
            throw new ClinetException(
                "Cannot delete this movie because it has scheduled projections. Remove the projections first.");
        }

        if (entity.MovieState == nameof(ActiveMovieState))
        {
            await MovieState.GetMovieState(nameof(ActiveMovieState)).DeactivateAsync(id);
            entity = await _dbContext.Movies.FindAsync(id)
                ?? throw new KeyNotFoundException($"Movie with id {id} not found.");
        }

        var state = MovieState.GetMovieState(entity.MovieState);
        await state.DeleteAsync(id);
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
            .Include(m => m.Assets)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Movie with id {id} not found.");

        var response = _mapper.Map<MovieResponse>(entity);
        response.AllowedActions = await GetAllowedActionsAsync(id);

        return response;
    }
}
