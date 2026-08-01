using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using eCommerce.Services.Enums;

namespace eCommerce.Services.MovieStateMachine
{
    public class InitialMovieState : BaseMovieState
    {
        public InitialMovieState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<MovieResponse> InsertAsync(MovieInsertRequest request)
        {
            var entity = Mapper.Map<Movie>(request);
            entity.MovieState = MovieLifecycleState.Draft;
            entity.CreatedAt = DateTime.UtcNow;
            DbContext.Movies.Add(entity);
            await DbContext.SaveChangesAsync();
            return await MapWithReferencesAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return base.GetAllowedActions().Append(nameof(InsertAsync)).ToList();
        }
    }
}
