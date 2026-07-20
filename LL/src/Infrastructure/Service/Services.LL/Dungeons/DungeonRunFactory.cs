using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Events;
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

        var rand = new Random(seed);

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
                    .Where(node => node.RoomType == RoomType.Checkpoint)
                    .Select(node => node.Section)
                    .Distinct()
                    .Count(),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
        run.State.RunId = run.Id;

        var layout = CreateDungeonLayout(delve);
        run.Rooms = layout.Rooms;
        run.State.MapNodes = layout.Nodes;
        run.State.TraversedRoomIndexes = layout.Nodes.Count == 0 ? [] : [layout.Nodes[0].RoomIndex];
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        InitializeDelveState(run, dungeon, delve, rand);
        HydrateRooms(dungeon, run.Rooms, rand);
        return run;
    }

    private static DungeonLayout CreateDungeonLayout(DungeonDelveDefinition delve)
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
            BossConsequence = definition.BossConsequence,
            BossAspectId = definition.BossAspectId,
            Tags = definition.Tags.ToList(),
            NextRoomIndexes = definition.NextRoomIndexes.ToList()
        }).ToList();
        var rooms = delve.Nodes.Select((definition, index) => new RoomInstance
        {
            RoomIndex = index,
            Type = definition.RoomType,
            Status = RoomInstanceStatus.Pending
        }).ToList();
        return new DungeonLayout(rooms, nodes);
    }

    private sealed record DungeonLayout(List<RoomInstance> Rooms, List<DungeonMapNode> Nodes);
    private static void InitializeDelveState(
        DungeonRun run,
        DungeonDefinition dungeon,
        DungeonDelveDefinition delve,
        Random random)
    {
        run.State.ActiveOmens = delve.Omens.Select(omen => new DungeonOmen
        {
            Id = omen.Id,
            Name = omen.Name,
            Description = omen.Description,
            CombatTollModifier = omen.CombatTollModifier,
            HazardTollModifier = omen.HazardTollModifier
        }).ToList();

        var tier = dungeon.Id.EndsWith("_iii", StringComparison.OrdinalIgnoreCase) ? 3
            : dungeon.Id.EndsWith("_ii", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        run.State.ActiveOmens = run.State.ActiveOmens
            .OrderBy(_ => random.Next())
            .Take(tier == 3 ? 2 : 1)
            .ToList();

        run.State.BossAspects = delve.BossAspects
        .Where(aspect => aspect.MinimumTier <= tier)
        .Select(aspect => new DungeonBossAspect
        {
            Id = aspect.Id,
            Name = aspect.Name,
            Description = aspect.Description,
            Source = aspect.Source,
            AttributeType = aspect.AttributeType,
            Amount = aspect.Amount,
            ModifierType = aspect.ModifierType
        }).ToList();
    }


    private static void HydrateRooms(DungeonDefinition dungeon, List<RoomInstance> rooms, Random rand)
    {
        foreach (var room in rooms)
        {
            if (room.Type == RoomType.Checkpoint)
                continue;

            if (room.Type is RoomType.Entrance or RoomType.Hazard or RoomType.Cache or RoomType.OmenSite)
                continue;

            if (room.Type == RoomType.Event)
            {
                ResolveEventFromTemplate(room, rand);
                continue;
            }

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

        var usesCombatFallback = roomType == RoomType.MiniBoss && template.Type == RoomType.Combat;
        if (template.Type != roomType && !usesCombatFallback)
            throw new InvalidOperationException(
                $"Template type mismatch. Expected '{roomType}', got '{template.Type}'.");

        if (template.EncounterIds is null || template.EncounterIds.Count == 0)
            throw new InvalidOperationException($"Room template for '{roomType}' has no encounters.");

        // Sanitize + de-dup while preserving order (important for authored boss compositions)
        var pool = new List<string>(template.EncounterIds.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in template.EncounterIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            var trimmed = id.Trim();
            if (seen.Add(trimmed))
                pool.Add(trimmed);
        }

        if (pool.Count == 0)
            throw new InvalidOperationException($"Room template for '{roomType}' only contained empty encounter ids.");

        // Catalogs without a dedicated miniboss variant use a full combat squad
        // while keeping the authored node a Miniboss.
        if (usesCombatFallback)
        {
            var count = Math.Min(3, pool.Count);
            var result = new List<string>(count);
            for (var index = 0; index < count; index++)
            {
                var pick = rand.Next(pool.Count);
                result.Add(pool[pick]);
                pool.RemoveAt(pick);
            }

            return result;
        }

        // Authored MiniBoss/Boss compositions are taken as-is.
        if (roomType is RoomType.MiniBoss or RoomType.Boss)
            return [.. pool];

        // Regular Combat: pick a random number of monsters from the pool
        if (roomType == RoomType.Combat)
        {
            // Decide how many to pick.
            // Opinionated defaults:
            // - If pool has 1 => always 1
            // - If pool has 2 => pick 1..2
            // - If pool >=3 => pick 1..min(3, pool.Count) (prevents silly 8-mob rooms early)
            int maxPick =
                pool.Count <= 2 ? pool.Count :
                Math.Min(3, pool.Count);

            int countToPick = rand.Next(1, maxPick + 1);

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

        // For other room types, encounters usually aren't applicable
        // (Checkpoint, Treasure, Shrine, Trap, Event, etc.)
        return [];
    }

    private static void ResolveEventFromTemplate(RoomInstance room, Random rand)
    {
        var eventTable = new EventTableDefinition();
        var totalWeight = eventTable.Outcomes.Sum(x => Math.Max(0, x.Weight));
        if (totalWeight <= 0)
        {
            room.EventOutcome = EventOutcomeType.TreasureRoom;
            return;
        }

        var roll = rand.Next(1, totalWeight + 1);
        var accumulated = 0;

        foreach (var outcome in eventTable.Outcomes)
        {
            accumulated += Math.Max(0, outcome.Weight);
            if (roll <= accumulated)
            {
                room.EventOutcome = outcome.Type;
                return;
            }
        }

        room.EventOutcome = eventTable.Outcomes[^1].Type;
    }

    private static RoomDefinition PickRoomVariantByType(DungeonDefinition dungeon, RoomType type, Random rand)
    {
        var pool = dungeon.Rooms
            .Where(r => r.Type == type)
            .ToList();

        if (pool.Count == 0 && type == RoomType.MiniBoss)
        {
            pool = dungeon.Rooms
                .Where(room => room.Type == RoomType.Combat)
                .ToList();
        }

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
