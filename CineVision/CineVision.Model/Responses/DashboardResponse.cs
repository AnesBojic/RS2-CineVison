namespace CineVision.Model.Responses
{
    /// <summary>
    /// High-level snapshot of CineVision's activity shown on the desktop dashboard.
    /// </summary>
    public class DashboardResponse
    {
        /// <summary>Collected, not-yet-refunded revenue on this month's current and upcoming shows.</summary>
        public decimal TotalRevenue { get; set; }
        /// <summary>Occupied seats on this month's current and upcoming (not-yet-ended) shows.</summary>
        public int TotalTicketsSold { get; set; }
        /// <summary>Distinct occupied bookings on this month's current and upcoming shows.</summary>
        public int TotalReservations { get; set; }

        /// <summary>Active user accounts with the Customer role.</summary>
        public int TotalCustomers { get; set; }

        public int TotalMovies { get; set; }
        /// <summary>Halls whose status currently allows projections (active screens).</summary>
        public int TotalScreens { get; set; }
        /// <summary>Movies with a non-cancelled projection that has not yet ended (now showing / upcoming).</summary>
        public int ActiveMovies { get; set; }

        /// <summary>Non-cancelled projections this month that have not ended yet.</summary>
        public int TotalProjections { get; set; }
        /// <summary>Non-cancelled projections this month that have not started yet.</summary>
        public int UpcomingProjections { get; set; }

        /// <summary>Average occupancy across this month's current and upcoming shows, as a percentage (0-100).</summary>
        public double AverageOccupancyPercent { get; set; }

        /// <summary>Best performing movies this month by revenue (top 5).</summary>
        public List<MoviePerformanceResponse> TopMovies { get; set; } = new();
    }
}
