using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<NotificationResponse>> GetForUserAsync(int userId, int limit = 50);
        Task<NotificationResponse> CreateAsync(int userId, string title, string message, string type);
        Task MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllReadAsync(int userId, string? type = null);
    }
}
