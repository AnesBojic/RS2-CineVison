using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    /// <summary>Shared fields of every reference-data insert/update request.</summary>
    public abstract class LookupRequest
    {
        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
