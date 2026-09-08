namespace CineVision.Model.SearchObjects
{
    public class ReservationSearchObject : BaseSearchObject
    {
        /// <summary>When set, filters reservations by status enum underlying value.</summary>
        public int? Status { get; set; }

        /// <summary>When set, filters reservations by projection id.</summary>
        public int? ProjectionId { get; set; }

        /// <summary>
        /// Ticket number, movie title, customer name or email. Applied on the server so a
        /// search is not limited to the current page.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>When set, filters by refund outcome (None / Pending / Refunded / Failed).</summary>
        public int? RefundStatus { get; set; }
    }
}
