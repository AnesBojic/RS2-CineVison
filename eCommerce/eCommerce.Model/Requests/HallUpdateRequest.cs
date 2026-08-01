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

        /// <summary>Id of a row in the ScreenTypes reference table.</summary>
        [Required]
        public int ScreenTypeId { get; set; }

        /// <summary>Id of a row in the HallStatuses reference table.</summary>
        [Required]
        public int StatusId { get; set; }
    }
}
