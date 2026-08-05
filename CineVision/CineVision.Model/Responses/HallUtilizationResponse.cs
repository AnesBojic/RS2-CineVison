namespace CineVision.Model.Responses
{
    /// <summary>
    /// How intensively a hall is being used: seats sold versus seats offered across its projections.
    /// </summary>
    public class HallUtilizationResponse
    {
        public int HallId { get; set; }
        public string HallName { get; set; } = string.Empty;

        /// <summary>Number of active seats in the hall.</summary>
        public int Capacity { get; set; }
        public int ProjectionsCount { get; set; }

        /// <summary>Number of projections (shows) scheduled in this hall. Mirrors <see cref="ProjectionsCount"/>.</summary>
        public int ShowCount { get; set; }

        /// <summary>This hall's projections divided by the total projections across all halls (0-100), for the usage-distribution pie chart.</summary>
        public double SharePercent { get; set; }

        /// <summary>Capacity multiplied by the number of projections in the hall.</summary>
        public int SeatsOffered { get; set; }
        public int SeatsSold { get; set; }

        /// <summary>Seats sold divided by seats offered (0-100).</summary>
        public double UtilizationPercent { get; set; }
    }
}
