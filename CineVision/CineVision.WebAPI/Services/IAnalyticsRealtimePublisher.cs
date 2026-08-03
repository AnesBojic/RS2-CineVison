using System.Threading.Tasks;

namespace CineVision.WebAPI.Services
{
    public interface IAnalyticsRealtimePublisher
    {
        Task BroadcastSnapshotAsync();
        Task SendSnapshotToClient(string connectionId);
    }
}
