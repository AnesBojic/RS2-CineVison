namespace CineVision.Model.Requests
{
    /// <summary>
    /// Upload or replace only the movie poster without sending the full movie update payload.
    /// </summary>
    public class MoviePosterUpdateRequest
    {
        public string? PosterImageBase64 { get; set; }
    }
}
