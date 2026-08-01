namespace eCommerce.Services.Enums
{
    /// <summary>Category of an in-app notification, used for filtering and for the client icon.</summary>
    public enum NotificationType
    {
        Email = 0,
        Message = 1,
        Reservation = 2,
        Payment = 3,
        Cancellation = 4,
        Status = 5
    }
}
