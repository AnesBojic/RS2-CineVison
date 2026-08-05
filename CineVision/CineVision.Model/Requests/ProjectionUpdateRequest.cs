using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class ProjectionUpdateRequest
    {
        [Required]
        public int MovieId { get; set; }

        [Required]
        public int HallId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public decimal BasePrice { get; set; }

        /// <summary>Id of a row in the Languages reference table.</summary>
        public int? LanguageId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
