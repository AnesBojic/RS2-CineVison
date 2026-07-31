using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IMovieService : IBaseCRUDService<MovieResponse, MovieSearchObject, MovieInsertRequest, MovieUpdateRequest>
    {
        Task<MovieResponse> ActivateAsync(int id);
        Task<MovieResponse> DeactivateAsync(int id);
        Task<List<string>> GetAllowedActionsAsync(int id);

        /// <summary>Increments the movie's view counter (called when its details are opened).</summary>
        Task RegisterViewAsync(int id);

        /// <summary>Sets or clears the movie poster image.</summary>
        Task<MovieResponse> UpdatePosterAsync(int id, MoviePosterUpdateRequest request);

        /// <summary>Preview of related rows removed by cascade delete.</summary>
        Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id);
    }
}
