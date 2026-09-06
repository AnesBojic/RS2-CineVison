using CineVision.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly HubConnectionTracker _connectionTracker;

    public NotificationsHub(HubConnectionTracker connectionTracker)
    {
        _connectionTracker = connectionTracker;
    }

    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        _connectionTracker.Track(Context);

        var userId = HubConnectionTracker.ResolveUserId(Context.User);
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.Forget(Context);

        var userId = HubConnectionTracker.ResolveUserId(Context.User);
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
