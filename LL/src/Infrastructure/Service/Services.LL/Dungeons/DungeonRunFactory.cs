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

        var layout = CreateDungeonLayout(delve, dungeon, layoutRandom);
        run.Rooms = layout.Rooms;
        run.State.MapNodes = layout.Nodes;
        run.State.TraversedRoomIndexes = layout.Nodes.Count == 0 ? [] : [layout.Nodes[0].RoomIndex];
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        HydrateRooms(dungeon, run.Rooms, encounterRandom);
        return run;
    }

    private static DungeonLayout CreateDungeonLayout(
        DungeonDelveDefinition delve,
        DungeonDefinition dungeon,
        Random random)
    {
        var rows = ResizeEncounterRows(delve, dungeon, random);
        var selectedDefinitions = rows.SelectMany((row, depth) =>
            SelectEncounterNodes(row, random).Select(definition => (Definition: definition, Depth: depth)))
            .ToList();
        var nodes = selectedDefinitions.Select((entry, index) => new DungeonMapNode
        {
            Id = $"{entry.Definition.Id}-depth-{entry.Depth}",
            DisplayName = entry.Definition.DisplayName,
            RoomIndex = index,
            Depth = entry.Depth,
            Lane = entry.Definition.Lane,
            Section = entry.Definition.Section,
            Forecast = entry.Definition.Forecast,
            VigorCostMin = entry.Definition.VigorCostMin,
            VigorCostMax = entry.Definition.VigorCostMax,
            NextRoomIndexes = []
        }).ToList();
        var rooms = selectedDefinitions.Select((entry, index) => new RoomInstance
        {
            RoomIndex = index,
            Type = entry.Definition.RoomType,
            Status = RoomInstanceStatus.Pending
        }).ToList();

        ConfigureRestSiteChoices(nodes, rooms, dungeon.RestSiteCount, random);
        ConfigureTreasuryChoices(nodes, rooms, dungeon, random);
        RandomizeLayout(nodes, rooms, random);
        return new DungeonLayout(rooms, nodes);
    }

    private sealed record DungeonLayout(List<RoomInstance> Rooms, List<DungeonMapNode> Nodes);

    private static List<List<DungeonDelveNodeDefinition>> ResizeEncounterRows(
        DungeonDelveDefinition delve,
        DungeonDefinition dungeon,
        Random random)
    {
        if (dungeon.MinRooms <= 0 || dungeon.MaxRooms < dungeon.MinRooms)
        {
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has an invalid room-count range.");
        }

        var rows = delve.Nodes
            .GroupBy(node => node.Depth)
            .OrderBy(row => row.Key)
            .Select(row => row.ToList())
            .ToList();
        var combatRows = rows.Where(IsCombatRow).ToList();

        // Length counts one room per depth, including Entrance and Boss, not alternative branches.
        var targetCount = (int)random.NextInt64(dungeon.MinRooms, (long)dungeon.MaxRooms + 1);
        while (rows.Count > targetCount)
        {
            // Keep at least one combat row in each stretch between authored special rooms.
            var removableIndexes = Enumerable.Range(0, rows.Count)
                .Where(index => IsCombatRow(rows[index]) &&
                    ((index > 0 && IsSameCombatStretch(rows[index - 1], rows[index])) ||
                     (index + 1 < rows.Count && IsSameCombatStretch(rows[index], rows[index + 1]))))
                .ToList();
            if (removableIndexes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Dungeon '{dungeon.Id}' cannot fit {targetCount} rooms without removing required layout rooms.");
            }

            rows.RemoveAt(removableIndexes[random.Next(removableIndexes.Count)]);
        }

        while (rows.Count < targetCount)
        {
            if (combatRows.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Dungeon '{dungeon.Id}' cannot reach {targetCount} rooms without combat row templates.");
            }

            var template = combatRows[random.Next(combatRows.Count)];
            rows.Insert(rows.LastIndexOf(template) + 1, template);
        }

        return rows;
    }

    private static bool IsCombatRow(List<DungeonDelveNodeDefinition> row) =>
        row.All(node => node.RoomType == RoomType.Combat);

    private static bool IsSameCombatStretch(
        List<DungeonDelveNodeDefinition> first,
        List<DungeonDelveNodeDefinition> second) =>
        IsCombatRow(first) && IsCombatRow(second) && first[0].Section == second[0].Section;

    private static List<DungeonDelveNodeDefinition> SelectEncounterNodes(
        IReadOnlyList<DungeonDelveNodeDefinition> definitions,
        Random random)
    {
        if (definitions.Count <= 1 || definitions.Any(node => node.RoomType != RoomType.Combat))
        {
            return definitions.ToList();
        }

        var candidates = definitions.ToList();
        var targetCount = RollEncounterRowWidth(candidates.Count, random);
        Shuffle(candidates, random);
        var selected = candidates.Take(targetCount).ToHashSet();
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
            < 0.475d => 2,
            < 0.80d => 3,
            _ => Math.Min(4, maximumWidth)
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

    private static void ConfigureTreasuryChoices(
        List<DungeonMapNode> nodes,
        List<RoomInstance> rooms,
        DungeonDefinition dungeon,
        Random random)
    {
        if (dungeon.TreasuryCount == 0) return;
        if (dungeon.TreasuryCount < 0 || dungeon.TreasuryVigorCost is < 1 or >= 100)
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has invalid Treasury settings.");

        var rows = nodes.GroupBy(node => node.Depth)
            .Where(row => row.Key > 1 && row.All(node => rooms[node.RoomIndex].Type == RoomType.Combat))
            .Select(row => row.ToList()).ToList();
        if (rows.Count < dungeon.TreasuryCount)
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has too few combat rows for its Treasuries.");
        Shuffle(rows, random);

        foreach (var row in rows.Take(dungeon.TreasuryCount))
        {
            DungeonMapNode treasury;
            if (row.Count == 4)
            {
                treasury = row[random.Next(row.Count)];
                rooms[treasury.RoomIndex].Type = RoomType.Treasury;
            }
            else
            {
                treasury = new DungeonMapNode
                {
                    RoomIndex = rooms.Count,
                    Depth = row[0].Depth,
                    Section = row[0].Section
                };
                nodes.Add(treasury);
                rooms.Add(new RoomInstance { RoomIndex = treasury.RoomIndex, Type = RoomType.Treasury });
                row.Add(treasury);
            }

            treasury.Id = $"treasury-depth-{treasury.Depth}";
            treasury.DisplayName = "Sealed Treasury";
            treasury.Forecast = "Spend Vigor for one guaranteed dungeon blueprint or equipment item. Added to Pending Loot.";
            treasury.VigorCostMin = treasury.VigorCostMax = dungeon.TreasuryVigorCost;
            Shuffle(row, random);
            for (var index = 0; index < row.Count; index++)
                row[index].Lane = index;
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
                rooms[node.RoomIndex].Type is RoomType.RestSite or RoomType.Treasury))
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
            if (room.Type is RoomType.RestSite or RoomType.Treasury)
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
