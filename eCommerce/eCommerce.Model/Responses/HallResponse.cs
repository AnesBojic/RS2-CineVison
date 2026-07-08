namespace eCommerce.Model.Responses
{
    public class HallResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int ScreenType { get; set; }
        public string ScreenTypeName { get; set; } = string.Empty;

        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int SeatCount { get; set; }

        /// <summary>Total seating capacity of the hall (equal to <see cref="SeatCount"/>).</summary>
        public int Capacity { get; set; }

        public List<SeatResponse> Seats { get; set; } = new List<SeatResponse>();
    }
}
