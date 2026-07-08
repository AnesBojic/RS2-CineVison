using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class HallInsertRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Screen technology: 0 = Standard, 1 = IMAX, 2 = 3D.</summary>
        public int ScreenType { get; set; } = 0;

        /// <summary>Operational status: 0 = Active, 1 = Maintenance, 2 = Inactive.</summary>
        public int Status { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When greater than zero, the hall is created with an auto-generated seat grid:
        /// rows labelled A, B, C... each containing <see cref="SeatsPerRow"/> seats.
        /// </summary>
        public int RowsCount { get; set; } = 0;

        public int SeatsPerRow { get; set; } = 0;
    }
}
