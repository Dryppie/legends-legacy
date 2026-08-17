using StackExchange.Redis;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace API.Chat.Hubs.Presence;

public sealed class RedisChatPresenceTracker : IChatPresenceTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _usersKey;
    private readonly string _userConnectionPrefix;
    private readonly string _connectionPrefix;
    private readonly long _leaseMilliseconds;
    private readonly ConcurrentDictionary<string, string> _localConnections = new(StringComparer.Ordinal);

    public RedisChatPresenceTracker(
        IConnectionMultiplexer redis,
        IOptions<RedisChatPresenceOptions> options)
    {
        _redis = redis;
        var configured = options.Value;
        var keyPrefix = configured.KeyPrefix.Trim().TrimEnd(':');
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            throw new InvalidOperationException("ChatPresence:KeyPrefix cannot be empty.");
        }
        if (configured.LeaseDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("ChatPresence:LeaseDuration must be positive.");
        }

        _usersKey = $"{keyPrefix}:{{presence}}:users";
        _userConnectionPrefix = $"{keyPrefix}:{{presence}}:user-connections:";
        _connectionPrefix = $"{keyPrefix}:{{presence}}:connection:";
        _leaseMilliseconds = checked((long)configured.LeaseDuration.TotalMilliseconds);
    }

    private const string ConnectScript = """
        local redisTime = redis.call('TIME')
        local now = (redisTime[1] * 1000) + math.floor(redisTime[2] / 1000)
        redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[3])
        redis.call('SADD', KEYS[2], ARGV[2])
        redis.call('ZADD', KEYS[3], now + ARGV[3], ARGV[1])
        redis.call('ZREMRANGEBYSCORE', KEYS[3], '-inf', now)
        return redis.call('ZCARD', KEYS[3])
        """;

    private const string DisconnectScript = """
        local redisTime = redis.call('TIME')
        local now = (redisTime[1] * 1000) + math.floor(redisTime[2] / 1000)
        redis.call('DEL', KEYS[1])
        redis.call('SREM', KEYS[2], ARGV[2])
        local connections = redis.call('SMEMBERS', KEYS[2])
        for _, connectionId in ipairs(connections) do
            if redis.call('EXISTS', ARGV[3] .. connectionId) == 0 then
                redis.call('SREM', KEYS[2], connectionId)
            end
        end
        if redis.call('SCARD', KEYS[2]) == 0 then
            redis.call('DEL', KEYS[2])
            redis.call('ZREM', KEYS[3], ARGV[1])
        end
        redis.call('ZREMRANGEBYSCORE', KEYS[3], '-inf', now)
        return redis.call('ZCARD', KEYS[3])
        """;

    private const string CountScript = """
        local redisTime = redis.call('TIME')
        local now = (redisTime[1] * 1000) + math.floor(redisTime[2] / 1000)
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now)
        return redis.call('ZCARD', KEYS[1])
        """;

    private const string RenewScript = """
        if redis.call('GET', KEYS[1]) ~= ARGV[1] then
            return 0
        end
        local redisTime = redis.call('TIME')
        local now = (redisTime[1] * 1000) + math.floor(redisTime[2] / 1000)
        redis.call('PEXPIRE', KEYS[1], ARGV[2])
        redis.call('ZADD', KEYS[2], now + ARGV[2], ARGV[1])
        return 1
        """;

    public async Task<int> GetOnlineUserCountAsync()
    {
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(
            CountScript,
            [_usersKey]);
        return checked((int)(long)result);
    }

    public async Task<int> ConnectAsync(string userId, string connectionId)
    {
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(
            ConnectScript,
            [ConnectionKey(connectionId), UserConnectionKey(userId), _usersKey],
            [userId, connectionId, _leaseMilliseconds]);
        _localConnections[connectionId] = userId;
        return checked((int)(long)result);
    }

    public async Task<int> DisconnectAsync(string userId, string connectionId)
    {
        _localConnections.TryRemove(connectionId, out _);
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(
            DisconnectScript,
            [ConnectionKey(connectionId), UserConnectionKey(userId), _usersKey],
            [userId, connectionId, _connectionPrefix]);
        return checked((int)(long)result);
    }

    public async Task RenewLocalConnectionsAsync()
    {
        var database = _redis.GetDatabase();
        var renewals = _localConnections.Select(connection =>
            database.ScriptEvaluateAsync(
                RenewScript,
                [ConnectionKey(connection.Key), _usersKey],
                [connection.Value, _leaseMilliseconds]));
        await Task.WhenAll(renewals);
    }

    private RedisKey UserConnectionKey(string userId) => $"{_userConnectionPrefix}{userId}";
    private RedisKey ConnectionKey(string connectionId) => $"{_connectionPrefix}{connectionId}";
}
