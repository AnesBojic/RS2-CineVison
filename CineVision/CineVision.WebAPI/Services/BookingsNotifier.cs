using CineVision.Services;
using CineVision.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Services;

public class BookingsNotifier : IBookingsNotifier
{
    private readonly IHubContext<BookingsHub> _hubContext;

    public BookingsNotifier(IHubContext<BookingsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBookingsChangedAsync()
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("BookingsUpdated");
        }
        catch
        {
            // Live bookings must never break checkout or cancel.
        }
    }
}
