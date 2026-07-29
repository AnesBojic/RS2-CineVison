using System.Threading.Tasks;

namespace eCommerce.WebAPI.Services
{
    public interface IAnalyticsRealtimePublisher
    {
        Task BroadcastSnapshotAsync();
        Task SendSnapshotToClient(string connectionId);
    }
}
