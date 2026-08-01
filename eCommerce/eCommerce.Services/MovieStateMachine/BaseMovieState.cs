using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.Services.MovieStateMachine
{
    public class BaseMovieState
    {
        protected ECommerceDbContext DbContext { get; }
        protected IMapper Mapper { get; }
        protected IServiceProvider ServiceProvider { get; }

        public BaseMovieState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
        {
            DbContext = dbContext;
            Mapper = mapper;
            ServiceProvider = serviceProvider;
        }

        public virtual Task<MovieResponse> InsertAsync(MovieInsertRequest request)
        {
            throw new InvalidOperationException("Cannot insert a movie in its current state.");
        }

        public virtual Task<MovieResponse> UpdateAsync(int id, MovieUpdateRequest request)
        {
            throw new InvalidOperationException("Cannot update a movie in its current state.");
        }

        public virtual Task<MovieResponse> ActivateAsync(int id)
        {
            throw new InvalidOperationException("Cannot activate a movie in its current state.");
        }

        public virtual Task<MovieResponse> DeactivateAsync(int id)
        {
            throw new InvalidOperationException("Cannot deactivate a movie in its current state.");
        }

        public virtual Task<MovieResponse> DeleteAsync(int id)
        {
            throw new InvalidOperationException("Cannot delete a movie in its current state.");
        }

        /// <summary>
        /// Maps a just-saved movie to a response with its reference rows loaded, so the genre,
        /// language and age rating labels are present instead of null.
        /// </summary>
        protected async Task<MovieResponse> MapWithReferencesAsync(Movie entity)
        {
            var entry = DbContext.Entry(entity);
            await entry.Reference(m => m.Genre).LoadAsync();
            await entry.Reference(m => m.Language).LoadAsync();
            await entry.Reference(m => m.AgeRating).LoadAsync();

            return Mapper.Map<MovieResponse>(entity);
        }

        public BaseMovieState GetMovieState(string stateName)
        {
            switch (stateName)
            {
                case nameof(InitialMovieState):
                    return ServiceProvider.GetService<InitialMovieState>()!;
                case nameof(DraftMovieState):
                    return ServiceProvider.GetService<DraftMovieState>()!;
                case nameof(ActiveMovieState):
                    return ServiceProvider.GetService<ActiveMovieState>()!;
                default:
                    throw new InvalidOperationException($"Unknown movie state: {stateName}");
            }
        }

        public virtual List<string> GetAllowedActions()
        {
            return new List<string>();
        }
    }
}
