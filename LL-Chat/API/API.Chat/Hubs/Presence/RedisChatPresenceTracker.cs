using StackExchange.Redis;

namespace API.Chat.Hubs.Presence;

public sealed class RedisChatPresenceTracker(IConnectionMultiplexer redis) : IChatPresenceTracker
{
    private const string UsersKey = "legends-legacy:chat:presence:users";
    private const string ConnectionPrefix = "legends-legacy:chat:presence:connections:";

    private const string ConnectScript = """
        redis.call('SADD', KEYS[1], ARGV[1])
        redis.call('SADD', KEYS[2], ARGV[2])
        return redis.call('SCARD', KEYS[2])
        """;

    private const string DisconnectScript = """
        redis.call('SREM', KEYS[1], ARGV[1])
        if redis.call('SCARD', KEYS[1]) == 0 then
            redis.call('DEL', KEYS[1])
            redis.call('SREM', KEYS[2], ARGV[2])
        end
        return redis.call('SCARD', KEYS[2])
        """;

    public async Task<int> GetOnlineUserCountAsync() =>
        checked((int)await redis.GetDatabase().SetLengthAsync(UsersKey));

    public async Task<int> ConnectAsync(string userId, string connectionId)
    {
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            ConnectScript,
            [ConnectionKey(userId), UsersKey],
            [connectionId, userId]);
        return checked((int)(long)result);
    }

    public async Task<int> DisconnectAsync(string userId, string connectionId)
    {
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            DisconnectScript,
            [ConnectionKey(userId), UsersKey],
            [connectionId, userId]);
        return checked((int)(long)result);
    }

    private static RedisKey ConnectionKey(string userId) =>
        $"{ConnectionPrefix}{userId}";
}
