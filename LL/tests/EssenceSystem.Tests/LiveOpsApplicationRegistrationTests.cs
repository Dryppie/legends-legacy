using API.LiveOps.Hosting;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL;

namespace EssenceSystem.Tests;

public sealed class LiveOpsApplicationRegistrationTests
{
    [Fact]
    public void LiveOps_registers_only_administration_request_handlers()
    {
        var services = new ServiceCollection();

        services.AddLiveOpsApplication();

        var handlers = services
            .Where(descriptor =>
                descriptor.ServiceType.IsGenericType &&
                (descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IRequestHandler<>) ||
                 descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IRequestHandler<,>)))
            .ToList();

        Assert.Equal(8, handlers.Count);
        Assert.All(handlers, descriptor => Assert.StartsWith(
            "Application.UseCases.Administration",
            descriptor.ImplementationType?.Namespace,
            StringComparison.Ordinal));
    }

    [Fact]
    public void LiveOps_registers_state_sync_and_outbox_realtime_dependencies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLiveOpsServices(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IStateSyncService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IGameRealtimeBroadcaster));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IGameRealtimeImmediatePublisher));
    }
}
