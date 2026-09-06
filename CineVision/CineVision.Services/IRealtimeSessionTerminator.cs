namespace CineVision.Services;

/// <summary>
/// Closes a user's open SignalR connections. Hub authorization only runs during the handshake,
/// so a demoted or deactivated user would otherwise keep an established connection — and the
/// data pushed over it — until the socket happens to drop.
/// </summary>
public interface IRealtimeSessionTerminator
{
    void TerminateUserConnections(int userId);
}
