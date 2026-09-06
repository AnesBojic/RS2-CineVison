using System.Collections.Concurrent;
using System.Security.Claims;
using CineVision.Services;
using CineVision.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.SignalR;

namespace CineVision.WebAPI.Hubs;

/// <summary>
/// Holds the live hub connections per user so that revoking a session can also close them.
/// Registered as a singleton because connections outlive any request scope.
/// </summary>
public class HubConnectionTracker : IRealtimeSessionTerminator
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, HubCallerContext>> _connections = new();

    public void Track(HubCallerContext context)
    {
        var userId = ResolveUserId(context.User);
        if (userId is null)
        {
            return;
        }

        _connections
            .GetOrAdd(userId.Value, _ => new ConcurrentDictionary<string, HubCallerContext>())
            .TryAdd(context.ConnectionId, context);
    }

    public void Forget(HubCallerContext context)
    {
        var userId = ResolveUserId(context.User);
        if (userId is null || !_connections.TryGetValue(userId.Value, out var userConnections))
        {
            return;
        }

        userConnections.TryRemove(context.ConnectionId, out _);
        if (userConnections.IsEmpty)
        {
            _connections.TryRemove(userId.Value, out _);
        }
    }

    public void TerminateUserConnections(int userId)
    {
        if (!_connections.TryRemove(userId, out var userConnections))
        {
            return;
        }

        // Aborting forces the client to reconnect, which re-runs the handshake and therefore
        // the token-version check with the user's current role.
        foreach (var context in userConnections.Values)
        {
            context.Abort();
        }
    }

    public static int? ResolveUserId(ClaimsPrincipal? user)
    {
        var id = user?.FindFirstValue(ClaimNames.Id)
                 ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId) ? userId : null;
    }
}
