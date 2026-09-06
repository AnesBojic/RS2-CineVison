using CineVision.Model;
using CineVision.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Hubs
{
    [Authorize(Roles = RoleNames.AdminStaff)]
    public class AnalyticsHub : Hub
    {
        private readonly IAnalyticsRealtimePublisher _publisher;
        private readonly HubConnectionTracker _connectionTracker;

        public AnalyticsHub(IAnalyticsRealtimePublisher publisher, HubConnectionTracker connectionTracker)
        {
            _publisher = publisher;
            _connectionTracker = connectionTracker;
        }

        public override async Task OnConnectedAsync()
        {
            // Analytics is pushed to every connected client, so a demoted admin must not keep
            // this connection: it is tracked and aborted when the role changes.
            _connectionTracker.Track(Context);

            await _publisher.SendSnapshotToClient(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _connectionTracker.Forget(Context);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
