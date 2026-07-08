using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ScreeningUpdateRequest
    {
        [Required]
        public int MovieId { get; set; }

        [Required]
        public int HallId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public decimal BasePrice { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        public bool HasSubtitles { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
