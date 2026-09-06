using CineVision.Services;

namespace CineVision.WebAPI.Services
{
    public class AnalyticsNotifier : IAnalyticsNotifier
    {
        private readonly IAnalyticsRealtimePublisher _publisher;
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsNotifier(IAnalyticsRealtimePublisher publisher, IAnalyticsService analyticsService)
        {
            _publisher = publisher;
            _analyticsService = analyticsService;
        }

        public async Task NotifyAnalyticsChangedAsync()
        {
            try
            {
                // Drop the 30s snapshot before SignalR rebuilds it, otherwise clients get stale KPIs.
                _analyticsService.InvalidateSnapshot();
                await _publisher.BroadcastSnapshotAsync();
            }
            catch
            {
                // Live analytics must never break bookings or reviews.
            }
        }
    }
}
