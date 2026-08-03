using CineVision.Services;

namespace CineVision.WebAPI.Services
{
    public class AnalyticsNotifier : IAnalyticsNotifier
    {
        private readonly IAnalyticsRealtimePublisher _publisher;

        public AnalyticsNotifier(IAnalyticsRealtimePublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task NotifyAnalyticsChangedAsync()
        {
            try
            {
                await _publisher.BroadcastSnapshotAsync();
            }
            catch
            {
                // Live analytics must never break bookings or reviews.
            }
        }
    }
}
