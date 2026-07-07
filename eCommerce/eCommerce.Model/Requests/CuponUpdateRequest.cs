using System;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class CuponUpdateRequest
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public double DiscountAmount { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
        public int Uses { get; set; } = 1;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsActive { get; set; } = true;
    }
}
