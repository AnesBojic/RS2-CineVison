namespace CineVision.Model.SearchObjects
{
    public class SeatSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter seats by the hall they belong to.
        /// </summary>
        public int? HallId { get; set; }

        /// <summary>
        /// Filter seats by seat type underlying value (0 = Regular, 1 = VIP).
        /// </summary>
        public int? SeatType { get; set; }
    }
}
