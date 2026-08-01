namespace eCommerce.Model.Enums
{
    /// <summary>Lifecycle of a booking; transitions are enforced by ReservationStatusTransitions.</summary>
    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Paid = 2,
        Cancelled = 3,
        Completed = 4
    }
}
