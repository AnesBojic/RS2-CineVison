using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class SeatUpdateRequest
    {
        [Required]
        [MaxLength(5)]
        public string RowLabel { get; set; } = string.Empty;

        public int SeatNumber { get; set; }

        /// <summary>0 = Regular, 1 = VIP.</summary>
        public int SeatType { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
