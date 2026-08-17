using API.Chat.Hubs.Presence;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Chat.Tests;

public sealed class RedisChatPresenceTrackerTests
{
    [Fact]
    public async Task Abandoned_connection_expires_from_online_count()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_REDIS_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var tracker = CreateTracker(redis, TimeSpan.FromMilliseconds(300));

        Assert.Equal(1, await tracker.ConnectAsync("user-1", "connection-1"));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.Equal(0, await tracker.GetOnlineUserCountAsync());
    }

    [Fact]
    public async Task Renewed_connection_remains_online_until_disconnected()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_REDIS_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var tracker = CreateTracker(redis, TimeSpan.FromMilliseconds(400));

        Assert.Equal(1, await tracker.ConnectAsync("user-1", "connection-1"));
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        await tracker.RenewLocalConnectionsAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, await tracker.GetOnlineUserCountAsync());
        Assert.Equal(0, await tracker.DisconnectAsync("user-1", "connection-1"));
    }

    private static RedisChatPresenceTracker CreateTracker(
        IConnectionMultiplexer redis,
        TimeSpan leaseDuration) =>
        new(
            redis,
            Options.Create(new RedisChatPresenceOptions
            {
                KeyPrefix = $"legends-legacy:chat:presence:test:{Guid.NewGuid():N}",
                LeaseDuration = leaseDuration,
                LeaseRenewalInterval = leaseDuration / 3
            }));
}
