namespace CineVision.Model.Responses
{
    /// <summary>
    /// Sales / occupancy aggregated into fixed daily time slots, backing the
    /// "Performance by Time Slot" chart on the analytics screen.
    /// </summary>
    public class TimeSlotPerformanceResponse
    {
        /// <summary>Human-readable slot label, e.g. "6:00 PM - 9:00 PM".</summary>
        public string TimeSlot { get; set; } = string.Empty;

        public int TicketsSold { get; set; }

        /// <summary>Seats sold divided by seats offered for screenings in this slot (0-100).</summary>
        public double OccupancyPercent { get; set; }

        public decimal Revenue { get; set; }
    }
}
