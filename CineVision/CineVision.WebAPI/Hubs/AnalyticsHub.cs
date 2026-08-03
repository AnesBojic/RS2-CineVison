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

        public AnalyticsHub(IAnalyticsRealtimePublisher publisher)
        {
            _publisher = publisher;
        }

        public override async Task OnConnectedAsync()
        {
            await _publisher.SendSnapshotToClient(Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}
