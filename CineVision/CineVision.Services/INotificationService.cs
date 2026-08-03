using CineVision.Model.Responses;
using CineVision.Services.Database;
using CineVision.Model.Enums;

namespace CineVision.Services
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
