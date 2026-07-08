using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class CreatePaymentIntentRequest
    {
        [Required]
        public int ScreeningId { get; set; }

        [Required]
        public List<int> SeatIds { get; set; } = new();
    }
}
