namespace API.Chat.Hubs.Presence;

// Chat currently runs as a single instance. Replace this with a shared
// Redis-backed tracker when the SignalR backplane is enabled.
public sealed class InMemoryChatPresenceTracker : IChatPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<string>> _connectionsByUser =
        new(StringComparer.OrdinalIgnoreCase);

    public int OnlineUserCount
    {
        get
        {
            lock (_gate)
            {
                return _connectionsByUser.Count;
            }
        }
    }

    public int Connect(string userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connections))
            {
                connections = new HashSet<string>(StringComparer.Ordinal);
                _connectionsByUser[userId] = connections;
            }

            connections.Add(connectionId);
            return _connectionsByUser.Count;
        }
    }

    public int Disconnect(string userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connections))
            {
                return _connectionsByUser.Count;
            }

            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _connectionsByUser.Remove(userId);
            }

            return _connectionsByUser.Count;
        }
    }
}
