namespace CineVision.Model.Responses
{
    public class HallResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int ScreenTypeId { get; set; }
        public string ScreenTypeName { get; set; } = string.Empty;

        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;

        /// <summary>Copied from the hall's status so clients can tell whether it can be scheduled.</summary>
        public bool AllowsProjections { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int SeatCount { get; set; }

        /// <summary>Physical people-capacity: regular active = 1, couple primary = 2, inactive partner = 0.</summary>
        public int Capacity { get; set; }

        /// <summary>Number of seat rows (A, B, C…).</summary>
        public int RowCount { get; set; }

        /// <summary>Number of seat columns per row.</summary>
        public int SeatsPerRow { get; set; }

        public List<SeatResponse> Seats { get; set; } = new List<SeatResponse>();
    }
}
