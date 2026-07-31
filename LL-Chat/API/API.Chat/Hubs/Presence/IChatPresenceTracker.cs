namespace API.Chat.Hubs.Presence;

public interface IChatPresenceTracker
{
    int OnlineUserCount { get; }
    int Connect(string userId, string connectionId);
    int Disconnect(string userId, string connectionId);
}
