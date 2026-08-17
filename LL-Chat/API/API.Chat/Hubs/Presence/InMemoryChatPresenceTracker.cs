namespace API.Chat.Hubs.Presence;

// Chat currently runs as a single instance. Replace this with a shared
// Redis-backed tracker when the SignalR backplane is enabled.
public sealed class InMemoryChatPresenceTracker : IChatPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<string>> _connectionsByUser =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<int> GetOnlineUserCountAsync()
    {
        lock (_gate) return Task.FromResult(_connectionsByUser.Count);
    }

    public Task<int> ConnectAsync(string userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connections))
            {
                connections = new HashSet<string>(StringComparer.Ordinal);
                _connectionsByUser[userId] = connections;
            }

            connections.Add(connectionId);
            return Task.FromResult(_connectionsByUser.Count);
        }
    }

    public Task<int> DisconnectAsync(string userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connections))
            {
                return Task.FromResult(_connectionsByUser.Count);
            }

            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _connectionsByUser.Remove(userId);
            }

            return Task.FromResult(_connectionsByUser.Count);
        }
    }
}
