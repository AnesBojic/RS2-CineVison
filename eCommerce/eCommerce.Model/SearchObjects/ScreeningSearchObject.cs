namespace eCommerce.Model.SearchObjects
{
    public class ScreeningSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter screenings by movie id.
        /// </summary>
        public int? MovieId { get; set; }

        /// <summary>
        /// Filter screenings by hall id.
        /// </summary>
        public int? HallId { get; set; }

        /// <summary>
        /// Only include screenings starting at or after this moment (UTC).
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Only include screenings starting at or before this moment (UTC).
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// When true, only returns screenings that have not started yet.
        /// </summary>
        public bool? OnlyUpcoming { get; set; }

        public bool? IncludeMovie { get; set; }

        public bool? IncludeHall { get; set; }
    }
}
