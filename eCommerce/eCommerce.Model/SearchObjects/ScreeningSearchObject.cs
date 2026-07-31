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

        /// <summary>
        /// When true, includes MoviePosterBase64 on list items. Default false.
        /// </summary>
        public bool? IncludePoster { get; set; }

        /// <summary>
        /// When true, loads hall seats and reservation seats to compute availability.
        /// Leave false for admin list views to keep queries fast.
        /// </summary>
        public bool? IncludeSeatStats { get; set; }

        /// <summary>
        /// When true, includes soft-cancelled (IsActive=false) projections. Default false.
        /// </summary>
        public bool? IncludeInactive { get; set; }
    }
}
