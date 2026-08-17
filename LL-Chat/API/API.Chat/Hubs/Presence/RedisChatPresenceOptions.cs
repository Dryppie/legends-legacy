namespace API.Chat.Hubs.Presence;

public sealed class RedisChatPresenceOptions
{
    public string KeyPrefix { get; set; } = "legends-legacy:chat:presence:v2";
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(90);
    public TimeSpan LeaseRenewalInterval { get; set; } = TimeSpan.FromSeconds(30);
}
