
using eCommerce.Model.Enums;
using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    public class Cupon
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public double DiscountAmount { get; set; }
        public DiscountType DiscountType { get; set; }
        public int Uses { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for orders using this coupon
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
