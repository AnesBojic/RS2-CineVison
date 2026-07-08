namespace eCommerce.Model.SearchObjects
{
    public class GenreSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring to match against the genre name (case-insensitive).
        /// </summary>
        public string? Name { get; set; }
    }
}
