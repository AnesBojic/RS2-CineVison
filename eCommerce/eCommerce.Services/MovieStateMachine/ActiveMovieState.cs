using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using eCommerce.Services.Enums;

namespace eCommerce.Services.MovieStateMachine
{
    public class ActiveMovieState : BaseMovieState
    {
        public ActiveMovieState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<MovieResponse> DeactivateAsync(int id)
        {
            var entity = await DbContext.Movies.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Movie with id {id} not found.");
            }

            entity.MovieState = MovieLifecycleState.Draft;
            await DbContext.SaveChangesAsync();

            return await MapWithReferencesAsync(entity);
        }

        public override async Task<MovieResponse> UpdateAsync(int id, MovieUpdateRequest request)
        {
            var entity = await DbContext.Movies.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Movie with id {id} not found.");
            }

            Mapper.Map(request, entity);
            entity.UpdatedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();

            return await MapWithReferencesAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(UpdateAsync), nameof(DeactivateAsync) };
        }
    }
}
