using CineVision.Model.Responses;

namespace CineVision.Services;

/// <summary>
/// Pushes in-app notification updates to a specific user over SignalR.
/// </summary>
public interface INotificationPushNotifier
{
    Task NotifyUserAsync(int userId, NotificationResponse notification, int unreadCount);

    Task NotifyUnreadCountAsync(int userId, int unreadCount);
}
