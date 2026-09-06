using System.Threading.Tasks;

namespace CineVision.Services
{
    /// <summary>
    /// Pushes a lightweight ping to desktop Bookings clients (SignalR) when reservation rows change.
    /// </summary>
    public interface IBookingsNotifier
    {
        Task NotifyBookingsChangedAsync();
    }
}
