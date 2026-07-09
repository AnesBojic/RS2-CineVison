namespace eCommerce.Model.Responses
{
    /// <summary>
    /// High-level snapshot of CineVision's activity shown on the desktop dashboard.
    /// </summary>
    public class DashboardResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalReservations { get; set; }

        public int TotalMovies { get; set; }
        public int ActiveMovies { get; set; }

        public int TotalScreenings { get; set; }
        public int UpcomingScreenings { get; set; }

        /// <summary>Average share of seats sold across all screenings, as a percentage (0-100).</summary>
        public double AverageOccupancyPercent { get; set; }

        /// <summary>Best performing movies by revenue (top 5).</summary>
        public List<MoviePerformanceResponse> TopMovies { get; set; } = new();
    }
}
