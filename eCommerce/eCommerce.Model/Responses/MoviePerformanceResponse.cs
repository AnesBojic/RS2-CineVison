namespace eCommerce.Model.Responses
{
    /// <summary>
    /// Sales/occupancy performance of a single movie across its screenings.
    /// </summary>
    public class MoviePerformanceResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ScreeningsCount { get; set; }
        public int ReservationsCount { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }

        /// <summary>Seats sold divided by seats offered across the movie's screenings (0-100).</summary>
        public double OccupancyPercent { get; set; }

        /// <summary>Average review rating (1-5) for the movie, or null when it has no reviews.</summary>
        public double? AvgRating { get; set; }
    }
}
