using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Dungeons.Mastery;

namespace Services.LL.Dungeons;

public sealed class DungeonRouteService : IDungeonRouteService
{
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

        var currentNode = run.State.MapNodes
            .FirstOrDefault(node => node.RoomIndex == run.CurrentRoomIndex);
        if (currentNode is not null)
        {
            var targetRooms = currentNode.NextRoomIndexes
                .Select(index => run.Rooms.FirstOrDefault(room => room.RoomIndex == index))
                .Where(room => room is not null)
                .Cast<RoomInstance>()
                .ToList();

            if (targetRooms.Count == 0)
            {
                return [];
            }

            var widenForecast = run.State.VigorState is "Strained" or "Exhausted";
            var vigorReduction = DungeonMasteryBenefits
                .Resolve(run.State.MasteryLevelAtStart)
                .CombatVigorCostReduction;
            var graphOptions = targetRooms
                .Select(room =>
                {
                    var node = run.State.MapNodes.First(candidate => candidate.RoomIndex == room.RoomIndex);
                    var isBoss = room.Type == RoomType.Boss;
                    var scaledVigorCostMin = isBoss
                        ? 0
                        : Math.Max(
                            0,
                            DungeonVigorService.ScaleCombatToll(node.VigorCostMin) - vigorReduction);
                    var scaledVigorCostMax = isBoss
                        ? 0
                        : Math.Max(
                            0,
                            DungeonVigorService.ScaleCombatToll(node.VigorCostMax) - vigorReduction);
                    var vigorCostMin = scaledVigorCostMin;
                    var vigorCostMax = scaledVigorCostMax;
                    if (!isBoss && widenForecast)
                    {
                        vigorCostMin = Math.Max(0, scaledVigorCostMin - 2);
                        vigorCostMax = Math.Min(35, scaledVigorCostMax + 2);
                    }

                    return new DungeonRouteOption
                    {
                        Id = $"route:{room.RoomIndex}:{node.Id}",
                        RoomIndex = room.RoomIndex,
                        DisplayName = string.IsNullOrWhiteSpace(node.DisplayName) ? node.Id : node.DisplayName,
                        RoomType = room.Type,
                        RiskLevel = scaledVigorCostMax switch
                        {
                            >= 28 => 4,
                            >= 22 => 3,
                            >= 15 => 2,
                            _ => 1
                        },
                        VigorCostMin = vigorCostMin,
                        VigorCostMax = vigorCostMax,
                        Forecast = node.Forecast
                    };
                })
                .ToList();
            run.State.CurrentRouteOptions = graphOptions;
            return graphOptions;
        }

        return [];
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
        var selectedNode = run.State.MapNodes
            .FirstOrDefault(node => node.RoomIndex == route.RoomIndex);
        if (selectedNode is not null)
        {
            run.State.CurrentSection = Math.Clamp(
                selectedNode.Section,
                1,
                Math.Max(1, run.State.TotalSections));
        }
        if (!run.State.TraversedRoomIndexes.Contains(route.RoomIndex))
        {
            run.State.TraversedRoomIndexes.Add(route.RoomIndex);
        }
        run.State.CurrentRouteOptions.Clear();
        return route;
    }
}
