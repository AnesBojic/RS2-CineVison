using System.Threading.Tasks;

namespace eCommerce.Services
{
  /// <summary>
  /// Pushes live analytics updates to connected desktop clients (SignalR).
  /// </summary>
  public interface IAnalyticsNotifier
  {
    Task NotifyAnalyticsChangedAsync();
  }
}
