namespace CineVision.Model.Responses
{
    /// <summary>
    /// How intensively a hall is being used: seats sold versus seats offered across its screenings.
    /// </summary>
    public class HallUtilizationResponse
    {
        public int HallId { get; set; }
        public string HallName { get; set; } = string.Empty;

        /// <summary>Number of active seats in the hall.</summary>
        public int Capacity { get; set; }
        public int ScreeningsCount { get; set; }

        /// <summary>Number of screenings (shows) scheduled in this hall. Mirrors <see cref="ScreeningsCount"/>.</summary>
        public int ShowCount { get; set; }

        /// <summary>This hall's screenings divided by the total screenings across all halls (0-100), for the usage-distribution pie chart.</summary>
        public double SharePercent { get; set; }

        /// <summary>Capacity multiplied by the number of screenings in the hall.</summary>
        public int SeatsOffered { get; set; }
        public int SeatsSold { get; set; }

        /// <summary>Seats sold divided by seats offered (0-100).</summary>
        public double UtilizationPercent { get; set; }
    }
}
