using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;

namespace eCommerce.Services.MovieStateMachine
{
    public class DraftMovieState : BaseMovieState
    {
        public DraftMovieState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
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

        public override async Task<MovieResponse> ActivateAsync(int id)
        {
            var entity = await DbContext.Movies.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Movie with id {id} not found.");
            }

            entity.MovieState = nameof(ActiveMovieState);
            await DbContext.SaveChangesAsync();

            return await MapWithReferencesAsync(entity);
        }

        public override async Task<MovieResponse> DeleteAsync(int id)
        {
            var entity = await DbContext.Movies.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Movie with id {id} not found.");
            }

            DbContext.Movies.Remove(entity);
            await DbContext.SaveChangesAsync();

            return Mapper.Map<MovieResponse>(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(UpdateAsync), nameof(ActivateAsync), nameof(DeleteAsync) };
        }
    }
}
