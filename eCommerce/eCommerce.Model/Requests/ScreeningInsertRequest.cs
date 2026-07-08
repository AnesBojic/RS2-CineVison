using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ScreeningInsertRequest
    {
        [Required]
        public int MovieId { get; set; }

        [Required]
        public int HallId { get; set; }

        /// <summary>Projection start time (UTC).</summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>Base ticket price per seat.</summary>
        [Required]
        public decimal BasePrice { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        public bool HasSubtitles { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
