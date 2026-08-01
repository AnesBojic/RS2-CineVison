using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.Enums;

namespace eCommerce.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<NotificationResponse>> GetForUserAsync(int userId, int limit = 50);
        Task<NotificationResponse> CreateAsync(int userId, string title, string message, NotificationType type);
        Task MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllReadAsync(int userId, NotificationType? type = null);
    }
}
