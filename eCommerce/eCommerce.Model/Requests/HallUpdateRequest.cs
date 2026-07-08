using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class HallUpdateRequest
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
    }
}
