using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using MapsterMapper;
using CineVision.Model.Enums;

namespace CineVision.Services.MovieStateMachine
{
    public class InitialMovieState : BaseMovieState
    {
        public InitialMovieState(CineVisionDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
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
