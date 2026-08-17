using API.LiveOps.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class LiveOpsPublicOriginTests
{
    [Fact]
    public async Task Configured_public_origin_replaces_internal_request_origin()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [LiveOpsPublicOrigin.ConfigurationKey] =
                    "https://liveops.legends-legacy.com"
            })
            .Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        string? observedOrigin = null;
        app.UseLiveOpsPublicOrigin(configuration);
        app.Run(context =>
        {
            observedOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
            return Task.CompletedTask;
        });
        var pipeline = app.Build();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("ll-app-ll-liveops");

        await pipeline(context);

        Assert.Equal("https://liveops.legends-legacy.com", observedOrigin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("liveops.legends-legacy.com")]
    [InlineData("https://operator@liveops.legends-legacy.com")]
    [InlineData("https://liveops.legends-legacy.com/path")]
    [InlineData("https://liveops.legends-legacy.com?query=value")]
    public void Invalid_public_origins_are_rejected(string value)
    {
        Assert.False(LiveOpsPublicOrigin.TryParse(value, out _));
    }
}
