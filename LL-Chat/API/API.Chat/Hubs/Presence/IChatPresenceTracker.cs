namespace API.Chat.Hubs.Presence;

public interface IChatPresenceTracker
{
    Task<int> GetOnlineUserCountAsync();
    Task<int> ConnectAsync(string userId, string connectionId);
    Task<int> DisconnectAsync(string userId, string connectionId);
}
