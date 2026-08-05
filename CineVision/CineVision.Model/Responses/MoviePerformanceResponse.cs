namespace CineVision.Model.Responses
{
    /// <summary>
    /// Sales/occupancy performance of a single movie across its projections.
    /// </summary>
    public class MoviePerformanceResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProjectionsCount { get; set; }
        public int ReservationsCount { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }

        /// <summary>Seats sold divided by seats offered across the movie's projections (0-100).</summary>
        public double OccupancyPercent { get; set; }

        /// <summary>Average review rating (1-5) for the movie, or null when it has no reviews.</summary>
        public double? AvgRating { get; set; }

        /// <summary>Optional poster for analytics / dashboard tables.</summary>
        public string? PosterImageBase64 { get; set; }
    }
}
