using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Chat.Tests;

public sealed class RedisSignalRBackplaneTests
{
    [Fact]
    public async Task Message_published_on_one_server_reaches_connection_on_another_server()
    {
        var redisConnection = Environment.GetEnvironmentVariable("LL_TEST_REDIS_CONNECTION");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            return;
        }

        var channelPrefix = RedisChannel.Literal($"legends-legacy:signalr:test:{Guid.NewGuid():N}");
        await using var firstServer = await StartServerAsync(redisConnection, channelPrefix);
        await using var secondServer = await StartServerAsync(redisConnection, channelPrefix);
        await using var connection = new HubConnectionBuilder()
            .WithUrl($"{GetAddress(firstServer)}/test-hub")
            .Build();

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string>("BackplaneMessage", message => received.TrySetResult(message));
        await connection.StartAsync();

        await secondServer.Services
            .GetRequiredService<IHubContext<BackplaneTestHub>>()
            .Clients.All
            .SendAsync("BackplaneMessage", "across-instances");

        Assert.Equal(
            "across-instances",
            await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    private static async Task<WebApplication> StartServerAsync(
        string redisConnection,
        RedisChannel channelPrefix)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services
            .AddSignalR()
            .AddStackExchangeRedis(redisConnection, options =>
                options.Configuration.ChannelPrefix = channelPrefix);

        var application = builder.Build();
        application.MapHub<BackplaneTestHub>("/test-hub");
        await application.StartAsync();
        return application;
    }

    private static string GetAddress(WebApplication application) =>
        application.Services
            .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();

    public sealed class BackplaneTestHub : Hub
    {
    }
}
