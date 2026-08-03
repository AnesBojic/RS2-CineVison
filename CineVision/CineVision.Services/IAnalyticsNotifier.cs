using System.Threading.Tasks;

namespace CineVision.Services
{
  /// <summary>
  /// Pushes live analytics updates to connected desktop clients (SignalR).
  /// </summary>
  public interface IAnalyticsNotifier
  {
    Task NotifyAnalyticsChangedAsync();
  }
}
