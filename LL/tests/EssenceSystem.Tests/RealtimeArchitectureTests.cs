using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;

namespace EssenceSystem.Tests;

public sealed class RealtimeArchitectureTests
{
    [Fact]
    public void Application_handlers_cannot_depend_on_the_immediate_realtime_transport()
    {
        var applicationAssembly = typeof(ICommandBase).Assembly;

        var offenders = applicationAssembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Where(type => type
                .GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter =>
                    parameter.ParameterType == typeof(IGameRealtimeImmediatePublisher)))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }
}
