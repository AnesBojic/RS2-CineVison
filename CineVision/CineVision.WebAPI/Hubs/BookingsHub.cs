using CineVision.Model;
using CineVision.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Hubs;

/// <summary>Desktop staff bookings list. AccessDesktop matches who can open that screen.</summary>
[Authorize(Policy = RolePermissionNames.AccessDesktop)]
public class BookingsHub : Hub
{
    private readonly HubConnectionTracker _connectionTracker;

    public BookingsHub(HubConnectionTracker connectionTracker)
    {
        _connectionTracker = connectionTracker;
    }

    public override Task OnConnectedAsync()
    {
        _connectionTracker.Track(Context);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.Forget(Context);
        return base.OnDisconnectedAsync(exception);
    }
}
