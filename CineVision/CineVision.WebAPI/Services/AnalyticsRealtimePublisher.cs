using CineVision.Model.Responses;
using CineVision.Services;
using CineVision.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Services
{
    public class AnalyticsRealtimePublisher : IAnalyticsRealtimePublisher
    {
        private readonly IHubContext<AnalyticsHub> _hubContext;
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsRealtimePublisher(
            IHubContext<AnalyticsHub> hubContext,
            IAnalyticsService analyticsService)
        {
            _hubContext = hubContext;
            _analyticsService = analyticsService;
        }

        public async Task BroadcastSnapshotAsync()
        {
            var snapshot = await _analyticsService.GetLiveSnapshotAsync();
            await _hubContext.Clients.All.SendAsync("AnalyticsUpdated", snapshot);
        }

        public async Task SendSnapshotToClient(string connectionId)
        {
            var snapshot = await _analyticsService.GetLiveSnapshotAsync();
            await _hubContext.Clients.Client(connectionId).SendAsync("AnalyticsUpdated", snapshot);
        }
    }
}
