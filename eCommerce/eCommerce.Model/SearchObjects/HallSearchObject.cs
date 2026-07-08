namespace eCommerce.Model.SearchObjects
{
    public class HallSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring to match against the hall name (case-insensitive).
        /// </summary>
        public string? Name { get; set; }

        public bool? IncludeSeats { get; set; }
    }
}
