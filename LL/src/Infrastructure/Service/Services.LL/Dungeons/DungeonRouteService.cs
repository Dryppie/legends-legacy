using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Definitions.Routes;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonRouteService : IDungeonRouteService
{
    private readonly IDungeonRouteDefinitionProvider _routeDefinitions;

    private sealed record RouteTemplate(
        string Name,
        int RiskLevel,
        int PressureDelta,
        string[] Tags,
        string[] PossibleRewards);

    public DungeonRouteService(IDungeonRouteDefinitionProvider routeDefinitions)
    {
        _routeDefinitions = routeDefinitions;
    }

    public IReadOnlyList<DungeonRouteOption> GenerateRouteOptions(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };
        run.State.CurrentRouteOptions.Clear();

        if (run.Status != DungeonRunStatus.Active)
        {
            return [];
        }

        var currentRoom = run.Rooms.FirstOrDefault(x => x.RoomIndex == run.CurrentRoomIndex);
        if (currentRoom is null || currentRoom.Status != RoomInstanceStatus.Completed)
        {
            return [];
        }

        var nextRoomIndex = run.CurrentRoomIndex + 1;
        var nextRoom = run.Rooms.FirstOrDefault(x => x.RoomIndex == nextRoomIndex);
        if (nextRoom is null || nextRoom.Type == RoomType.Boss)
        {
            return [];
        }

        var seed = CreateRoomSeed(run.Seed, nextRoom.RoomIndex, run.State.Pressure);
        var options = CreateOptionsForRoom(run, nextRoom, new Random(seed));
        run.State.CurrentRouteOptions = options;
        return options;
    }

    public DungeonRouteOption ChooseRoute(DungeonRun run, string routeOptionId)
    {
        ArgumentNullException.ThrowIfNull(run);

        var route = run.State.CurrentRouteOptions
            .FirstOrDefault(x => string.Equals(x.Id, routeOptionId, StringComparison.OrdinalIgnoreCase));

        if (route is null)
        {
            throw new InvalidOperationException("The selected route is no longer available.");
        }

        run.CurrentRoomIndex = route.RoomIndex;
        run.State.CurrentRouteOptions.Clear();
        if (route.Id.StartsWith("hidden:", StringComparison.OrdinalIgnoreCase))
        {
            run.State.Flags["hidden_route_taken"] = run.State.Flags.GetValueOrDefault("hidden_route_taken") + 1;
            run.State.Flags.Remove("hidden_route_revealed");
        }

        return route;
    }

    private List<DungeonRouteOption> CreateOptionsForRoom(DungeonRun run, RoomInstance room, Random random)
    {
        var authored = _routeDefinitions.GetDefinitions(run.DungeonDefinitionId, room.Type);
        if (authored.Count > 0)
        {
            return authored
                .OrderBy(_ => random.Next())
                .Take(room.Type == RoomType.Checkpoint ? 2 : 3)
                .Select(definition => ToOption(room, definition))
                .ToList();
        }

        return CreateFallbackOptionsForRoom(room, random);
    }

    private static List<DungeonRouteOption> CreateFallbackOptionsForRoom(RoomInstance room, Random random)
    {
        List<RouteTemplate> templates = room.Type switch
        {
            RoomType.Event =>
            [
                new("Glittering Side Passage", 2, 8, ["event", "loot"], ["Treasure", "Boons"]),
                new("Collapsed Service Tunnel", 2, 4, ["event", "mystery"], ["Pressure relief"]),
                new("Echoing Detour", 3, 12, ["event", "danger"], ["Better rewards"])
            ],
            RoomType.MiniBoss =>
            [
                new("Lookout Post", 3, 12, ["elite", "boon"], ["Boon chance"]),
                new("Guarded Shortcut", 3, 8, ["elite"], ["Unsecured loot"]),
                new("Reckless Ambush", 4, 15, ["elite", "greedy"], ["Improved rewards"])
            ],
            RoomType.Checkpoint =>
            [
                new("Quiet Camp", 1, 0, ["checkpoint", "safe"], []),
                new("Restless Camp", 2, 4, ["checkpoint"], ["Focus opportunity"])
            ],
            _ =>
            [
                new("Narrow Tunnel", 1, 4, ["combat", "safe"], []),
                new("Loot-Strewn Passage", 2, 8, ["combat", "loot"], ["Extra cinders"]),
                new("Patrolled Hall", 3, 12, ["combat", "danger"], ["Better rewards"])
            ]
        };

        return templates
            .OrderBy(_ => random.Next())
            .Take(room.Type == RoomType.Checkpoint ? 2 : 3)
            .Select((template, index) => new DungeonRouteOption
            {
                Id = $"route:{room.RoomIndex}:{index + 1}",
                RoomIndex = room.RoomIndex,
                DisplayName = template.Name,
                RoomType = room.Type,
                RiskLevel = template.RiskLevel,
                PressureDelta = template.PressureDelta,
                Tags = template.Tags.ToList(),
                PossibleRewards = template.PossibleRewards.ToList(),
                IsUnknown = template.Tags.Contains("mystery")
            })
            .ToList();
    }

    private static DungeonRouteOption ToOption(RoomInstance room, DungeonRouteDefinition definition) => new()
    {
        Id = $"route:{room.RoomIndex}:{definition.Id}",
        RoomIndex = room.RoomIndex,
        DisplayName = definition.DisplayName,
        RoomType = room.Type,
        RiskLevel = definition.RiskLevel,
        PressureDelta = definition.PressureDelta,
        IsUnknown = definition.IsUnknown,
        Tags = definition.Tags.ToList(),
        PossibleRewards = definition.PossibleRewards.ToList(),
        Requirements = definition.Requirements.ToList()
    };

    private static int CreateRoomSeed(int runSeed, int roomIndex, int pressure)
    {
        unchecked
        {
            var seed = runSeed;
            seed = (seed * 397) ^ roomIndex;
            seed = (seed * 397) ^ pressure;
            seed = (seed * 397) ^ 71;
            return seed;
        }
    }
}
