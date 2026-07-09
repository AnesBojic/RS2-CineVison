namespace eCommerce.Model.SearchObjects
{
    /// <summary>
    /// Optional filters for analytics reports. When date bounds are omitted the whole
    /// history is considered.
    /// </summary>
    public class ReportSearchObject
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        /// <summary>Bucket size for revenue-over-time reports: "day", "week" or "month". Defaults to "day".</summary>
        public string? GroupBy { get; set; }
    }
}
