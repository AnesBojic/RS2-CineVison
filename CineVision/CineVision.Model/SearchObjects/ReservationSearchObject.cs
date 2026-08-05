namespace CineVision.Model.SearchObjects
{
    public class ReservationSearchObject : BaseSearchObject
    {
        /// <summary>When set, filters reservations by status enum underlying value.</summary>
        public int? Status { get; set; }

        /// <summary>When set, filters reservations by projection id.</summary>
        public int? ProjectionId { get; set; }
    }
}
