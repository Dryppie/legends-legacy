using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Services.LL.Interfaces;

namespace Services.LL.Dungeons;

public sealed class DungeonRunFactory
{
    private readonly IDungeonDefinitions _dungeons;
    private readonly ICharacterSnapshotService _snapshotService;
    private readonly IDungeonDelveDefinitionProvider _delves;

    public DungeonRunFactory(
        IDungeonDefinitions dungeons,
        ICharacterSnapshotService snapshots,
        IDungeonDelveDefinitionProvider delves)
    {
        _dungeons = dungeons;
        _snapshotService = snapshots;
        _delves = delves;
    }

    public async Task<DungeonRun> CreateAsync(Guid characterId, string dungeonDefinitionId, int seed, CancellationToken ct)
    {
        var dungeon = _dungeons.GetByKey(dungeonDefinitionId);
        var delve = _delves.GetForDungeon(dungeonDefinitionId);
        var snapshot = await _snapshotService.CreateAsync(characterId, ct);

        var layoutRandom = new Random(seed);
        var encounterRandom = new Random(seed);

        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            CharacterSnapshotId = snapshot.Id,
            DungeonDefinitionId = dungeonDefinitionId,
            DungeonDefinitionName = dungeon.Name,
            Seed = seed,
            Status = DungeonRunStatus.Active,
            CurrentRoomIndex = 0,
            State = new DungeonRunState
            {
                Vigor = 100,
                VigorState = "Steady",
                CurrentSection = 1,
                TotalSections = delve.Nodes
                    .Where(node => node.RoomType == RoomType.RestSite)
                    .Select(node => node.Section)
                    .Distinct()
                    .Count(),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
        run.State.RunId = run.Id;

        var layout = CreateDungeonLayout(delve, layoutRandom);
        run.Rooms = layout.Rooms;
        run.State.MapNodes = layout.Nodes;
        run.State.TraversedRoomIndexes = layout.Nodes.Count == 0 ? [] : [layout.Nodes[0].RoomIndex];
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        HydrateRooms(dungeon, run.Rooms, encounterRandom);
        return run;
    }

    private static DungeonLayout CreateDungeonLayout(
        DungeonDelveDefinition delve,
        Random random)
    {
        var nodes = delve.Nodes.Select((definition, index) => new DungeonMapNode
        {
            Id = definition.Id,
            DisplayName = definition.DisplayName,
            RoomIndex = index,
            Depth = definition.Depth,
            Lane = definition.Lane,
            Section = definition.Section,
            Forecast = definition.Forecast,
            VigorCostMin = definition.VigorCostMin,
            VigorCostMax = definition.VigorCostMax,
            NextRoomIndexes = definition.NextRoomIndexes.ToList()
        }).ToList();
        var rooms = delve.Nodes.Select((definition, index) => new RoomInstance
        {
            RoomIndex = index,
            Type = definition.RoomType,
            Status = RoomInstanceStatus.Pending
        }).ToList();

        RandomizeLayout(nodes, rooms, random);
        return new DungeonLayout(rooms, nodes);
    }

    private sealed record DungeonLayout(List<RoomInstance> Rooms, List<DungeonMapNode> Nodes);

    private static void RandomizeLayout(
        List<DungeonMapNode> nodes,
        IReadOnlyList<RoomInstance> rooms,
        Random random)
    {
        var rows = nodes
            .GroupBy(node => node.Depth)
            .OrderBy(group => group.Key)
            .Select(group => group.OrderBy(node => node.RoomIndex).ToList())
            .ToList();

        foreach (var row in rows.Where(row => row.Count > 1))
        {
            var encounterNodes = row
                .Where(node => rooms[node.RoomIndex].Type is RoomType.Combat or RoomType.MiniBoss)
                .ToList();
            if (encounterNodes.Count != row.Count)
            {
                continue;
            }

            var lanes = encounterNodes.Select(node => node.Lane).ToList();
            Shuffle(lanes, random);
            for (var index = 0; index < encounterNodes.Count; index++)
            {
                encounterNodes[index].Lane = lanes[index];
            }
        }

        foreach (var node in nodes)
        {
            node.NextRoomIndexes.Clear();
        }

        for (var rowIndex = 0; rowIndex + 1 < rows.Count; rowIndex++)
        {
            ConnectRows(rows[rowIndex], rows[rowIndex + 1], random);
        }
    }

    private static void ConnectRows(
        IReadOnlyList<DungeonMapNode> sourceRow,
        IReadOnlyList<DungeonMapNode> targetRow,
        Random random)
    {
        if (sourceRow.Count == 1)
        {
            sourceRow[0].NextRoomIndexes = targetRow
                .OrderBy(node => node.Lane)
                .ThenBy(node => node.RoomIndex)
                .Select(node => node.RoomIndex)
                .ToList();
            return;
        }

        if (targetRow.Count == 1)
        {
            foreach (var source in sourceRow)
            {
                source.NextRoomIndexes = [targetRow[0].RoomIndex];
            }

            return;
        }

        var shuffledSources = sourceRow.ToList();
        var shuffledTargets = targetRow.ToList();
        Shuffle(shuffledSources, random);
        Shuffle(shuffledTargets, random);

        for (var index = 0; index < shuffledSources.Count; index++)
        {
            shuffledSources[index].NextRoomIndexes.Add(
                shuffledTargets[index % shuffledTargets.Count].RoomIndex);
        }

        for (var index = shuffledSources.Count; index < shuffledTargets.Count; index++)
        {
            shuffledSources[index % shuffledSources.Count].NextRoomIndexes.Add(
                shuffledTargets[index].RoomIndex);
        }

        var additionalEdges = (
                from source in sourceRow
                from target in targetRow
                where !source.NextRoomIndexes.Contains(target.RoomIndex)
                select (Source: source, Target: target))
            .ToList();
        Shuffle(additionalEdges, random);

        var extraEdgeCount = random.Next(
            1,
            Math.Min(sourceRow.Count, additionalEdges.Count) + 1);
        foreach (var edge in additionalEdges.Take(extraEdgeCount))
        {
            edge.Source.NextRoomIndexes.Add(edge.Target.RoomIndex);
        }

        foreach (var source in sourceRow)
        {
            source.NextRoomIndexes = source.NextRoomIndexes
                .Distinct()
                .OrderBy(index => targetRow.Single(node => node.RoomIndex == index).Lane)
                .ThenBy(index => index)
                .ToList();
        }
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static void HydrateRooms(DungeonDefinition dungeon, List<RoomInstance> rooms, Random rand)
    {
        foreach (var room in rooms)
        {
            if (room.Type == RoomType.RestSite)
                continue;

            if (room.Type == RoomType.Entrance)
                continue;

            var template = PickRoomVariantByType(dungeon, room.Type, rand);

            HydrateRoomFromTemplate(room, template, rand);
        }
    }

    private static void HydrateRoomFromTemplate(RoomInstance room, RoomDefinition template, Random rand)
    {
        switch (room.Type)
        {
            case RoomType.Combat:
            case RoomType.MiniBoss:
            case RoomType.Boss:
                room.EncounterIds = ResolveEncountersFromTemplate(room.Type, template, rand);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(room.Type), room.Type, "Unsupported RoomType.");
        }
    }

    private static List<string> ResolveEncountersFromTemplate(RoomType roomType, RoomDefinition template, Random rand)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(rand);

        if (template.Type != roomType)
            throw new InvalidOperationException(
                $"Template type mismatch. Expected '{roomType}', got '{template.Type}'.");

        if (template.EncounterIds is null || template.EncounterIds.Count == 0)
            throw new InvalidOperationException($"Room template for '{roomType}' has no encounters.");

        var authoredEncounters = template.EncounterIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToList();

        if (authoredEncounters.Count == 0)
            throw new InvalidOperationException($"Room template for '{roomType}' only contained empty encounter ids.");

        // Authored MiniBoss/Boss compositions preserve order and repeated creatures.
        if (roomType is RoomType.MiniBoss or RoomType.Boss)
            return authoredEncounters;

        // Combat templates are random-selection pools, so duplicate keys add no value.
        var pool = authoredEncounters
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Regular Combat: pick 2-4 monsters when the authored pool allows it.
        if (roomType == RoomType.Combat)
        {
            var minPick = Math.Min(2, pool.Count);
            var maxPick = Math.Min(4, pool.Count);
            var countToPick = rand.Next(minPick, maxPick + 1);

            // Sample without replacement
            var result = new List<string>(countToPick);
            for (int i = 0; i < countToPick; i++)
            {
                int idx = rand.Next(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }

        return [];
    }

    private static RoomDefinition PickRoomVariantByType(DungeonDefinition dungeon, RoomType type, Random rand)
    {
        var pool = dungeon.Rooms
            .Where(r => r.Type == type)
            .ToList();

        if (pool.Count == 0)
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has no RoomDefinition variants for type '{type}'.");

        // Weighted roll (float weights)
        float total = 0f;
        foreach (var r in pool)
            total += MathF.Max(0f, r.Weight);

        if (total <= 0f)
            return pool[0];

        float roll = (float)rand.NextDouble() * total;
        float acc = 0f;

        foreach (var r in pool)
        {
            acc += MathF.Max(0f, r.Weight);
            if (roll <= acc)
                return r;
        }

        return pool[^1];
    }
}
