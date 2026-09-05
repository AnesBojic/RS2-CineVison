using System.ComponentModel.DataAnnotations;
using CineVision.Model.Enums;

namespace CineVision.Model.Requests
{
    public class ReservationCreateRequest
    {
        [Required]
        public int ProjectionId { get; set; }

        /// <summary>Identifiers of the seats the customer wants to reserve for the projection.</summary>
        [Required]
        public List<int> SeatIds { get; set; } = new();

        /// <summary>
        /// How the booking is paid. Defaults to <see cref="PaymentMethod.Online"/>, which requires a
        /// succeeded Stripe payment; <see cref="PaymentMethod.Counter"/> is an Admin/Staff box-office sale.
        /// </summary>
        public PaymentMethod? PaymentMethod { get; set; }

        /// <summary>
        /// Stripe payment intent of the seat hold being finalised. Required for online bookings.
        /// </summary>
        public string? PaymentIntentId { get; set; }

        /// <summary>Optional guest name captured at checkout (shown on the booking confirmation screen).</summary>
        public string? CustomerName { get; set; }

        /// <summary>Optional guest email captured at checkout (used for the confirmation email).</summary>
        public string? CustomerEmail { get; set; }
    }
}
