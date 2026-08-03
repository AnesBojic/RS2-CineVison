namespace CineVision.Model.Responses
{
    /// <summary>
    /// Counts of related records that will be removed when an admin deletes a user.
    /// </summary>
    public class UserDeleteImpactResponse
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
        public int ReviewCount { get; set; }
        public int NotificationCount { get; set; }
    }
}
