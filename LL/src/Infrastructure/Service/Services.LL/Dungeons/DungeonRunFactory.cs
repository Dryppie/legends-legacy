using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Services.LL.Interfaces;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Essences;

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
        ValidateRestSiteCount(dungeon, delve);
        var snapshot = await _snapshotService.CreateAsync(characterId, EssenceCombatActivity.Dungeon, ct);

        var startedWithoutWeapon = snapshot.Equipment.All(x => x.Slot != EquipmentSlotType.MainHand);
        var run = CreateRun(characterId, snapshot.Id, dungeon, delve, seed, startedWithoutWeapon);
        return run;
    }

    public DungeonRun CreateForSimulation(string dungeonDefinitionId, int seed)
    {
        var dungeon = _dungeons.GetByKey(dungeonDefinitionId);
        var delve = _delves.GetForDungeon(dungeonDefinitionId);
        ValidateRestSiteCount(dungeon, delve);

        return CreateRun(Guid.Empty, null, dungeon, delve, seed, false);
    }

    private static DungeonRun CreateRun(
        Guid characterId,
        Guid? snapshotId,
        DungeonDefinition dungeon,
        DungeonDelveDefinition delve,
        int seed,
        bool startedWithoutWeapon)
    {

        var layoutRandom = new Random(seed);
        var encounterRandom = new Random(seed);

        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            CharacterSnapshotId = snapshotId,
            DungeonDefinitionId = dungeon.Id,
            DungeonDefinitionName = dungeon.Name,
            Seed = seed,
            Status = DungeonRunStatus.Active,
            CurrentRoomIndex = 0,
            State = new DungeonRunState
            {
                StartedWithoutWeapon = startedWithoutWeapon,
                Vigor = 100,
                VigorState = "Steady",
                CurrentSection = 1,
                TotalSections = Math.Max(1, delve.Nodes.Max(node => node.Section)),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
        run.State.RunId = run.Id;

        var layout = CreateDungeonLayout(delve, dungeon.RestSiteCount, layoutRandom);
        run.Rooms = layout.Rooms;
        run.State.MapNodes = layout.Nodes;
        run.State.TraversedRoomIndexes = layout.Nodes.Count == 0 ? [] : [layout.Nodes[0].RoomIndex];
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        HydrateRooms(dungeon, run.Rooms, encounterRandom);
        return run;
    }

    private static DungeonLayout CreateDungeonLayout(
        DungeonDelveDefinition delve,
        int restSiteCount,
        Random random)
    {
        var selectedDefinitions = SelectEncounterNodes(delve.Nodes, random);
        var nodes = selectedDefinitions.Select((definition, index) => new DungeonMapNode
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
            NextRoomIndexes = []
        }).ToList();
        var rooms = selectedDefinitions.Select((definition, index) => new RoomInstance
        {
            RoomIndex = index,
            Type = definition.RoomType,
            Status = RoomInstanceStatus.Pending
        }).ToList();

        ConfigureRestSiteChoices(nodes, rooms, restSiteCount, random);
        RandomizeLayout(nodes, rooms, random);
        return new DungeonLayout(rooms, nodes);
    }

    private sealed record DungeonLayout(List<RoomInstance> Rooms, List<DungeonMapNode> Nodes);

    private static List<DungeonDelveNodeDefinition> SelectEncounterNodes(
        IReadOnlyList<DungeonDelveNodeDefinition> definitions,
        Random random)
    {
        var selected = new HashSet<DungeonDelveNodeDefinition>();

        foreach (var row in definitions.GroupBy(node => node.Depth))
        {
            var candidates = row.ToList();
            if (candidates.Count <= 1 || candidates.Any(node => node.RoomType != RoomType.Combat))
            {
                selected.UnionWith(candidates);
                continue;
            }

            var targetCount = RollEncounterRowWidth(candidates.Count, random);
            Shuffle(candidates, random);
            selected.UnionWith(candidates.Take(targetCount));
        }

        return definitions.Where(selected.Contains).ToList();
    }

    private static int RollEncounterRowWidth(int maximumWidth, Random random)
    {
        if (maximumWidth <= 1)
        {
            return maximumWidth;
        }

        var roll = random.NextDouble();
        if (maximumWidth == 2)
        {
            return roll < 0.10d ? 1 : 2;
        }

        return roll switch
        {
            < 0.075d => 1,
            < 0.70d => 2,
            _ => Math.Min(3, maximumWidth)
        };
    }

    private static void ValidateRestSiteCount(
        DungeonDefinition dungeon,
        DungeonDelveDefinition delve)
    {
        if (dungeon.RestSiteCount < 0)
        {
            throw new InvalidOperationException(
                $"Dungeon '{dungeon.Id}' cannot have a negative Rest Site count.");
        }

        var availableSlots = delve.Nodes.Count(node => node.RoomType == RoomType.RestSite);
        if (dungeon.RestSiteCount > availableSlots)
        {
            throw new InvalidOperationException(
                $"Dungeon '{dungeon.Id}' requests {dungeon.RestSiteCount} Rest Sites, " +
                $"but delve '{delve.Id}' only provides {availableSlots} Rest Site slots.");
        }
    }

    private static void ConfigureRestSiteChoices(
        List<DungeonMapNode> nodes,
        List<RoomInstance> rooms,
        int restSiteCount,
        Random random)
    {
        var restSiteSlots = nodes
            .Where(node => rooms[node.RoomIndex].Type == RoomType.RestSite)
            .OrderBy(node => node.RoomIndex)
            .ToList();
        var selectedSlots = restSiteSlots.ToList();
        Shuffle(selectedSlots, random);
        var selectedRoomIndexes = selectedSlots
            .Take(restSiteCount)
            .Select(node => node.RoomIndex)
            .ToHashSet();

        foreach (var restSite in restSiteSlots)
        {
            var (minimumVigorCost, maximumVigorCost) = GetSectionCombatCost(
                restSite.Section,
                nodes,
                rooms);
            var combatDisplayName = GetCombatAlternativeDisplayName(restSite.DisplayName);

            if (!selectedRoomIndexes.Contains(restSite.RoomIndex))
            {
                var room = rooms[restSite.RoomIndex];
                room.Type = RoomType.Combat;
                restSite.DisplayName = combatDisplayName;
                restSite.Forecast = "Fight through another encounter for additional rewards.";
                restSite.VigorCostMin = minimumVigorCost;
                restSite.VigorCostMax = maximumVigorCost;
                continue;
            }

            var restSiteLane = random.Next(2) == 0 ? -1 : 1;
            restSite.Lane = restSiteLane;
            var combatRoomIndex = rooms.Count;
            nodes.Add(new DungeonMapNode
            {
                Id = $"{restSite.Id}-combat",
                DisplayName = combatDisplayName,
                RoomIndex = combatRoomIndex,
                Depth = restSite.Depth,
                Lane = -restSiteLane,
                Section = restSite.Section,
                Forecast = "Fight through another encounter for additional rewards.",
                VigorCostMin = minimumVigorCost,
                VigorCostMax = maximumVigorCost
            });
            rooms.Add(new RoomInstance
            {
                RoomIndex = combatRoomIndex,
                Type = RoomType.Combat,
                Status = RoomInstanceStatus.Pending
            });
        }
    }

    private static (int Minimum, int Maximum) GetSectionCombatCost(
        int section,
        IReadOnlyCollection<DungeonMapNode> nodes,
        IReadOnlyList<RoomInstance> rooms)
    {
        var sectionCombatNodes = nodes
            .Where(node =>
                node.Section == section &&
                rooms[node.RoomIndex].Type == RoomType.Combat &&
                node.VigorCostMin > 0 &&
                node.VigorCostMax >= node.VigorCostMin)
            .ToList();

        if (sectionCombatNodes.Count == 0)
        {
            return (12, 22);
        }

        return (
            (int)Math.Round(sectionCombatNodes.Average(node => node.VigorCostMin), MidpointRounding.AwayFromZero),
            (int)Math.Round(sectionCombatNodes.Average(node => node.VigorCostMax), MidpointRounding.AwayFromZero));
    }

    private static string GetCombatAlternativeDisplayName(string restSiteDisplayName)
    {
        var location = restSiteDisplayName
            .Replace("Rest Site", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return string.IsNullOrWhiteSpace(location)
            ? "Guarded Passage"
            : $"{location} Guard";
    }

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
            ConnectRows(rows[rowIndex], rows[rowIndex + 1], rooms, random);
        }
    }

    private static void ConnectRows(
        IReadOnlyList<DungeonMapNode> sourceRow,
        IReadOnlyList<DungeonMapNode> targetRow,
        IReadOnlyList<RoomInstance> rooms,
        Random random)
    {
        if (sourceRow.Concat(targetRow).Any(node =>
                rooms[node.RoomIndex].Type == RoomType.RestSite))
        {
            var targetIndexes = targetRow
                .OrderBy(node => node.Lane)
                .ThenBy(node => node.RoomIndex)
                .Select(node => node.RoomIndex)
                .ToList();
            foreach (var source in sourceRow)
            {
                source.NextRoomIndexes = targetIndexes.ToList();
            }

            return;
        }

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
