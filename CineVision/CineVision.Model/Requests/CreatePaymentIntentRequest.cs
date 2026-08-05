using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class CreatePaymentIntentRequest
    {
        [Required]
        public int ProjectionId { get; set; }

        [Required]
        public List<int> SeatIds { get; set; } = new();
    }
}
