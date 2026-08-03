using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
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

        /// <summary>Id of a row in the Languages reference table.</summary>
        public int? LanguageId { get; set; }

        public bool HasSubtitles { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
