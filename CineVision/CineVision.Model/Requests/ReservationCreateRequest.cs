using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class ReservationCreateRequest
    {
        [Required]
        public int ScreeningId { get; set; }

        /// <summary>Identifiers of the seats the customer wants to reserve for the screening.</summary>
        [Required]
        public List<int> SeatIds { get; set; } = new();

        /// <summary>
        /// Optional Stripe payment intent id. When provided the reservation is marked as paid.
        /// </summary>
        public string? PaymentIntentId { get; set; }

        /// <summary>Optional guest name captured at checkout (shown on the booking confirmation screen).</summary>
        public string? CustomerName { get; set; }

        /// <summary>Optional guest email captured at checkout (used for the confirmation email).</summary>
        public string? CustomerEmail { get; set; }
    }
}
