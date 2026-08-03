namespace CineVision.Model.Responses
{
    /// <summary>
    /// Revenue and ticket counts aggregated into a time bucket (day, week or month).
    /// </summary>
    public class RevenueByPeriodResponse
    {
        /// <summary>Human-readable label for the bucket, e.g. "2026-07-05" or "2026-07".</summary>
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
        public int ReservationsCount { get; set; }
    }
}
