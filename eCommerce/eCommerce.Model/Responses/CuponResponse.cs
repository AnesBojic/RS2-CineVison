using System;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class CuponResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public double DiscountAmount { get; set; }
        public DiscountType DiscountType { get; set; }
        public int Uses { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
