namespace CineVision.Model.SearchObjects
{
    public class ProjectionSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter projections by movie id.
        /// </summary>
        public int? MovieId { get; set; }

        /// <summary>Filter projections by hall id.</summary>
        public int? HallId { get; set; }

        /// <summary>
        /// When true, only projections whose hall currently allows shows (not Maintenance/Inactive).
        /// Ignored when <see cref="HallId"/> is set.
        /// </summary>
        public bool? ActiveHallsOnly { get; set; }

        /// <summary>
        /// Only include projections starting at or after this moment (UTC).
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Only include projections starting at or before this moment (UTC).
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// When true, only returns projections that have not started yet.
        /// </summary>
        public bool? OnlyUpcoming { get; set; }

        /// <summary>
        /// Desktop list filter: <c>upcoming</c> (active, not started), <c>live</c>
        /// (not cancelled and not ended), <c>past</c>, <c>cancelled</c>.
        /// Null or any other value means all statuses.
        /// </summary>
        public string? Status { get; set; }

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
    }
}
