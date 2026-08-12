namespace CineVision.Model.SearchObjects
{
    public class MovieSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring to match against the movie title (case-insensitive).
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Substring to match against the movie description (case-insensitive).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Filter movies by genre id.
        /// </summary>
        public int? GenreId { get; set; }

        public bool? IncludeGenre { get; set; }

        /// <summary>
        /// When true, list items include PosterImageBase64. Default false keeps list payloads small.
        /// </summary>
        public bool? IncludePoster { get; set; }
    }
}
