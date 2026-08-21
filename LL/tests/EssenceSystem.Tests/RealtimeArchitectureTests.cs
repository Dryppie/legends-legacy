using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using System.Reflection;
using System.Text.RegularExpressions;

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

    [Fact]
    public void Backend_and_Angular_realtime_event_registries_stay_in_parity()
    {
        var backendEventTypes = typeof(GameRealtimeEvent).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(GameRealtimeEvent).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var backendNames = typeof(GameRealtimeEventNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue()))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(backendEventTypes, backendNames);

        var source = File.ReadAllText(FindFrontendContracts());
        var signalNames = ExtractStringValues(source, "gameRealtimeSignalEventNames");
        var frontendNames = ExtractStringValues(source, "gameRealtimeEventNames")
            .Concat(signalNames)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var signalMapNames = ExtractInterfaceKeys(source, "GameRealtimeSignalEventMap");

        Assert.Equal(backendNames, frontendNames);
        Assert.Equal(signalNames.Order(StringComparer.Ordinal), signalMapNames.Order(StringComparer.Ordinal));
    }

    private static IReadOnlyList<string> ExtractStringValues(string source, string constantName)
    {
        var marker = $"export const {constantName} = {{";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find {constantName} in the Angular contracts.");
        var end = source.IndexOf("} as const;", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Could not find the end of {constantName}.");
        var block = source[start..end];
        return Regex.Matches(block, @"\w+\s*:\s*'([^']+)'")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractInterfaceKeys(string source, string interfaceName)
    {
        var marker = $"export interface {interfaceName} {{";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find {interfaceName} in the Angular contracts.");
        var end = source.IndexOf('}', start);
        Assert.True(end > start, $"Could not find the end of {interfaceName}.");
        var block = source[start..end];
        return Regex.Matches(block, @"(?m)^\s*([A-Za-z]\w*)\s*:")
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static string FindFrontendContracts()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                foreach (var relativePath in new[]
                         {
                             Path.Combine("src", "Presentation", "ll", "src", "app", "core", "services", "real-time", "game-realtime", "game-realtime-contracts.ts"),
                             Path.Combine("LL", "src", "Presentation", "ll", "src", "app", "core", "services", "real-time", "game-realtime", "game-realtime-contracts.ts")
                         })
                {
                    var candidate = Path.Combine(current.FullName, relativePath);
                    if (File.Exists(candidate)) return candidate;
                }
                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate the Angular realtime contracts.");
    }
}
