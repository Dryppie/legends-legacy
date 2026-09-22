using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed record DungeonVigorFeasibilityResult(
    bool IsFeasible,
    int BestFinalVigor,
    int DeepestReachableDepth,
    int BestVigorAtDeepestDepth);

public static class DungeonVigorFeasibilityEvaluator
{
    public static DungeonVigorFeasibilityResult Evaluate(
        IReadOnlyCollection<DungeonMapNode> nodes,
        IReadOnlyList<RoomInstance> rooms,
        int masteryLevel)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(rooms);

        var roomByIndex = rooms.ToDictionary(room => room.RoomIndex);
        var nodeByIndex = nodes.ToDictionary(node => node.RoomIndex);
        var entrance = nodes.Single(node =>
            roomByIndex[node.RoomIndex].Type == RoomType.Entrance);
        var boss = nodes.Single(node =>
            roomByIndex[node.RoomIndex].Type == RoomType.Boss);
        var bestVigor = nodeByIndex.Keys.ToDictionary(index => index, _ => int.MinValue);
        bestVigor[entrance.RoomIndex] = 100;

        var deepestReachableDepth = entrance.Depth;
        var bestVigorAtDeepestDepth = 100;

        foreach (var node in nodes.OrderBy(node => node.Depth).ThenBy(node => node.RoomIndex))
        {
            var vigor = bestVigor[node.RoomIndex];
            if (vigor == int.MinValue)
            {
                continue;
            }

            if (node.Depth > deepestReachableDepth)
            {
                deepestReachableDepth = node.Depth;
                bestVigorAtDeepestDepth = vigor;
            }
            else if (node.Depth == deepestReachableDepth)
            {
                bestVigorAtDeepestDepth = Math.Max(bestVigorAtDeepestDepth, vigor);
            }

            foreach (var nextRoomIndex in node.NextRoomIndexes)
            {
                if (!nodeByIndex.TryGetValue(nextRoomIndex, out var nextNode) ||
                    !roomByIndex.TryGetValue(nextRoomIndex, out var nextRoom))
                {
                    continue;
                }

                var nextVigor = ResolveArrivalVigor(vigor, nextNode, nextRoom, masteryLevel);
                if (!nextVigor.HasValue)
                {
                    continue;
                }

                bestVigor[nextRoomIndex] = Math.Max(bestVigor[nextRoomIndex], nextVigor.Value);
            }
        }

        var bestFinalVigor = bestVigor[boss.RoomIndex];
        return new DungeonVigorFeasibilityResult(
            bestFinalVigor > 0,
            Math.Max(0, bestFinalVigor),
            deepestReachableDepth,
            bestVigorAtDeepestDepth);
    }

    private static int? ResolveArrivalVigor(
        int currentVigor,
        DungeonMapNode node,
        RoomInstance room,
        int masteryLevel)
    {
        switch (room.Type)
        {
            case RoomType.RestSite:
                return Math.Min(
                    100,
                    currentVigor + DungeonVigorService.CalculateRestSiteRecovery(masteryLevel));

            case RoomType.Combat:
            case RoomType.MiniBoss:
            {
                var remaining = currentVigor - DungeonVigorService.CalculateCombatToll(
                    node.VigorCostMin,
                    masteryLevel);
                return remaining > 0 ? remaining : null;
            }

            case RoomType.Treasury:
                // Treasuries are optional and must never be required for a feasible route.
                return null;

            default:
                return currentVigor;
        }
    }
}
