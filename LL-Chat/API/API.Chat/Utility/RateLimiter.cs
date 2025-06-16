using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace API.Chat.Utility;

public static class RateLimiter
{
    public static async Task<bool> EnsureAllowedAsync(IDistributedCache cache, string userId, string keyPrefix = "chat:limit", int limit = 5, int windowSeconds = 5)
    {
        var key = $"{keyPrefix}:{userId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var bucketJson = await cache.GetStringAsync(key);
        var timestamps = bucketJson != null
            ? JsonSerializer.Deserialize<List<long>>(bucketJson)
            : [];

        timestamps = [.. timestamps.Where(ts => ts > now - windowSeconds)];

        if (timestamps.Count >= limit)
        {
            return false; // Reached rate limit
        }

        timestamps.Add(now);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds)
        };

        await cache.SetStringAsync(key, JsonSerializer.Serialize(timestamps), options);
        return true;
    }
}
