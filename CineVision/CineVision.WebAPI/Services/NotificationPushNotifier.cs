using CineVision.Model.Responses;
using CineVision.Services;
using CineVision.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Services;

public class NotificationPushNotifier : INotificationPushNotifier
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public NotificationPushNotifier(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyUserAsync(int userId, NotificationResponse notification, int unreadCount)
    {
        await _hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId))
            .SendAsync("NotificationReceived", new
            {
                notification,
                unreadCount
            });
    }

    public async Task NotifyUnreadCountAsync(int userId, int unreadCount)
    {
        await _hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId))
            .SendAsync("UnreadCountUpdated", unreadCount);
    }
}
