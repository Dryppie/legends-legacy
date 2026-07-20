using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonDelveDefinitionProvider : IDungeonDelveDefinitionProvider
{
    private readonly IReadOnlyList<DungeonDelveDefinition> _definitions;

    public JsonDungeonDelveDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "dungeon-delves.json");
        var document = JsonSerializer.Deserialize<Document>(File.ReadAllText(path), options) ?? new();
        Validate(document.Delves);
        _definitions = document.Delves;
    }

    public DungeonDelveDefinition GetForDungeon(string dungeonDefinitionId) =>
        _definitions
            .OrderByDescending(definition => definition.DungeonDefinitionIds.Count)
            .FirstOrDefault(definition => definition.DungeonDefinitionIds.Any(id =>
                dungeonDefinitionId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                dungeonDefinitionId.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase)))
        ?? throw new KeyNotFoundException($"No Waypoint Delve definition matches '{dungeonDefinitionId}'.");

    public IReadOnlyList<DungeonDelveDefinition> GetAll() => _definitions;

    private static void Validate(IReadOnlyList<DungeonDelveDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("At least one dungeon delve definition is required.");
        }

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || definition.DungeonDefinitionIds.Count == 0)
                throw new InvalidOperationException("Every delve requires an id and dungeonDefinitionIds.");
            if (definition.Nodes.Count == 0 || definition.Nodes[0].RoomType != RoomType.Entrance)
                throw new InvalidOperationException($"Delve '{definition.Id}' must begin with an Entrance node.");
            if (definition.Nodes.Count(node => node.RoomType == RoomType.Entrance) != 1)
                throw new InvalidOperationException($"Delve '{definition.Id}' must contain exactly one Entrance node.");
            if (definition.Nodes.Count(node => node.RoomType == RoomType.Boss) != 1)
                throw new InvalidOperationException($"Delve '{definition.Id}' must contain exactly one boss.");
            if (definition.Nodes.All(node => node.RoomType != RoomType.RestSite))
                throw new InvalidOperationException($"Delve '{definition.Id}' must contain at least one Section ending in a Rest Site.");
            if (definition.Omens.Count is < 4 or > 6)
                throw new InvalidOperationException($"Delve '{definition.Id}' must author an Omen pool of four to six entries.");

            if (definition.Nodes.Any(node => node.NextRoomIndexes.Count > 3))
                throw new InvalidOperationException($"Delve '{definition.Id}' nodes may branch to at most three other nodes.");
            if (definition.Nodes
                .SelectMany((node, index) => node.NextRoomIndexes.Select(next => (index, next)))
                .Any(edge => edge.next <= edge.index))
                throw new InvalidOperationException($"Delve '{definition.Id}' cannot contain backtracking routes.");

            var indexes = Enumerable.Range(0, definition.Nodes.Count).ToHashSet();
            if (definition.Nodes.SelectMany(node => node.NextRoomIndexes).Any(index => !indexes.Contains(index)))
                throw new InvalidOperationException($"Delve '{definition.Id}' contains a route to a missing node.");

            ValidateSections(definition);
            ValidateReachability(definition);

            var aspectIds = definition.BossAspects.Select(aspect => aspect.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (definition.Nodes.Any(node => !string.IsNullOrWhiteSpace(node.BossAspectId) && !aspectIds.Contains(node.BossAspectId)))
                throw new InvalidOperationException($"Delve '{definition.Id}' contains a route linked to a missing boss Aspect.");
        }
    }

    private static void ValidateSections(DungeonDelveDefinition definition)
    {
        var indexedNodes = definition.Nodes
            .Select((node, index) => new IndexedNode(index, node))
            .ToList();
        var restSites = indexedNodes
            .Where(entry => entry.Node.RoomType == RoomType.RestSite)
            .OrderBy(entry => entry.Node.Depth)
            .ToList();
        var sectionNumbers = restSites
            .Select(entry => entry.Node.Section)
            .Order()
            .ToList();
        var expectedSections = Enumerable.Range(1, restSites.Count).ToList();

        if (!sectionNumbers.SequenceEqual(expectedSections))
        {
            throw new InvalidOperationException(
                $"Delve '{definition.Id}' must number its Rest Site Sections consecutively from 1.");
        }

        if (definition.Nodes.Any(node => node.Section < 1 || node.Section > restSites.Count))
        {
            throw new InvalidOperationException(
                $"Delve '{definition.Id}' contains a node outside its authored Section range.");
        }

        var entrance = indexedNodes[0];
        if (entrance.Node.Section != 1)
        {
            throw new InvalidOperationException($"Delve '{definition.Id}' Entrance must be in Section 1.");
        }

        for (var section = 1; section <= restSites.Count; section++)
        {
            var anchor = section == 1
                ? entrance
                : restSites.Single(entry => entry.Node.Section == section - 1);
            var restSite = restSites.Single(entry => entry.Node.Section == section);

            if (restSite.Node.Depth <= anchor.Node.Depth)
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} Rest Site must appear after its starting node.");
            }

            var misplacedNodes = indexedNodes
                .Where(entry =>
                    entry.Node.Depth > anchor.Node.Depth &&
                    entry.Node.Depth <= restSite.Node.Depth &&
                    entry.Node.Section != section)
                .ToList();
            if (misplacedNodes.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} contains nodes assigned to another Section.");
            }

            var rows = indexedNodes
                .Where(entry =>
                    entry.Node.Section == section &&
                    entry.Node.Depth > anchor.Node.Depth &&
                    entry.Node.Depth < restSite.Node.Depth)
                .GroupBy(entry => entry.Node.Depth)
                .OrderBy(group => group.Key)
                .Select(group => group.ToList())
                .ToList();

            if (rows.Count is < 1 or > 2)
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} must contain one or two encounter rows before its Rest Site.");
            }

            if (rows.Any(row => row.Count is < 1 or > 3))
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} encounter rows may contain at most three nodes.");
            }

            var expectedDepth = anchor.Node.Depth + 1;
            foreach (var row in rows)
            {
                if (row[0].Node.Depth != expectedDepth)
                {
                    throw new InvalidOperationException(
                        $"Delve '{definition.Id}' Section {section} encounter rows and Rest Site must use consecutive depths.");
                }

                expectedDepth++;
            }

            if (restSite.Node.Depth != expectedDepth ||
                indexedNodes.Count(entry => entry.Node.Depth == restSite.Node.Depth) != 1)
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} must end with a lone Rest Site immediately after its encounter rows.");
            }

            var firstRowIndexes = rows[0].Select(entry => entry.Index).ToHashSet();
            if (anchor.Node.NextRoomIndexes.Count is < 1 or > 3 ||
                !anchor.Node.NextRoomIndexes.ToHashSet().SetEquals(firstRowIndexes))
            {
                throw new InvalidOperationException(
                    $"Delve '{definition.Id}' Section {section} must fan from its start into every node of its first row.");
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var allowedTargets = rowIndex + 1 < rows.Count
                    ? rows[rowIndex + 1].Select(entry => entry.Index).ToHashSet()
                    : [restSite.Index];
                var usedTargets = new HashSet<int>();

                foreach (var entry in row)
                {
                    if (entry.Node.NextRoomIndexes.Count is < 1 or > 3 ||
                        entry.Node.NextRoomIndexes.Any(target => !allowedTargets.Contains(target)))
                    {
                        throw new InvalidOperationException(
                            $"Delve '{definition.Id}' Section {section} routes must advance to the next row or its Rest Site.");
                    }

                    usedTargets.UnionWith(entry.Node.NextRoomIndexes);
                }

                if (!usedTargets.SetEquals(allowedTargets))
                {
                    throw new InvalidOperationException(
                        $"Delve '{definition.Id}' Section {section} contains an unreachable encounter node or Rest Site.");
                }
            }
        }

        var finalRestSite = restSites[^1];
        var boss = indexedNodes.Single(entry => entry.Node.RoomType == RoomType.Boss);
        if (boss.Node.Depth <= finalRestSite.Node.Depth ||
            boss.Node.Section != restSites.Count ||
            boss.Node.NextRoomIndexes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Delve '{definition.Id}' boss must be terminal and appear after the final Section's Rest Site.");
        }

        if (indexedNodes.Any(entry =>
                entry.Node.Depth > finalRestSite.Node.Depth &&
                entry.Node.Section != restSites.Count))
        {
            throw new InvalidOperationException(
                $"Delve '{definition.Id}' boss approach must remain associated with the final Section.");
        }
    }

    private static void ValidateReachability(DungeonDelveDefinition definition)
    {
        var reachableFromEntrance = Traverse([0], index => definition.Nodes[index].NextRoomIndexes);
        if (reachableFromEntrance.Count != definition.Nodes.Count)
        {
            throw new InvalidOperationException($"Delve '{definition.Id}' contains a node that cannot be reached from its Entrance.");
        }

        var bossIndex = definition.Nodes.FindIndex(node => node.RoomType == RoomType.Boss);
        var reverseEdges = Enumerable.Range(0, definition.Nodes.Count)
            .ToDictionary(index => index, _ => new List<int>());
        for (var source = 0; source < definition.Nodes.Count; source++)
        {
            foreach (var target in definition.Nodes[source].NextRoomIndexes)
            {
                reverseEdges[target].Add(source);
            }
        }

        var canReachBoss = Traverse([bossIndex], index => reverseEdges[index]);
        if (canReachBoss.Count != definition.Nodes.Count)
        {
            throw new InvalidOperationException($"Delve '{definition.Id}' contains a route that cannot reach its boss.");
        }
    }

    private static HashSet<int> Traverse(
        IEnumerable<int> starts,
        Func<int, IEnumerable<int>> getNext)
    {
        var visited = new HashSet<int>();
        var pending = new Queue<int>(starts);

        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var next in getNext(current))
            {
                pending.Enqueue(next);
            }
        }

        return visited;
    }

    private sealed record IndexedNode(int Index, DungeonDelveNodeDefinition Node);

    private sealed class Document
    {
        public List<DungeonDelveDefinition> Delves { get; set; } = [];
    }
}
