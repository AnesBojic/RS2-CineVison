namespace eCommerce.Model.SearchObjects
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

        /// <summary>
        /// Filter movies by their current state machine state.
        /// </summary>
        public string? MovieState { get; set; }

        public bool? IncludeGenre { get; set; }

        public bool? IncludeAssets { get; set; }
    }
}
