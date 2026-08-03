using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class HallSeatLayoutUpdateRequest
    {
        [Required]
        public List<HallSeatLayoutItem> Seats { get; set; } = new();
    }

    public class HallSeatLayoutItem
    {
        public int SeatId { get; set; }

        /// <summary>0 = Regular, 2 = Couple (uses this seat + the seat on the right).</summary>
        public int SeatType { get; set; }
    }
}
