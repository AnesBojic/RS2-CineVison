using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class HallInsertRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Id of a row in the ScreenTypes reference table.</summary>
        [Required]
        public int ScreenTypeId { get; set; }

        /// <summary>Id of a row in the HallStatuses reference table.</summary>
        [Required]
        public int StatusId { get; set; }

        /// <summary>
        /// When greater than zero, the hall is created with an auto-generated seat grid:
        /// rows labelled A, B, C... each containing <see cref="SeatsPerRow"/> seats.
        /// </summary>
        public int RowsCount { get; set; } = 0;

        public int SeatsPerRow { get; set; } = 0;
    }
}
