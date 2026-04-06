using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Services.LL.Interfaces;
using Services.LL.JsonDefinitions;

namespace Services.LL.Dungeons;

public sealed class DungeonRunFactory
{
    private readonly IDungeonDefinitions _dungeons;
    private readonly ICharacterSnapshotService _snapshotService;

    public DungeonRunFactory(IDungeonDefinitions dungeons, ICharacterSnapshotService snapshots)
    {
        _dungeons = dungeons;
        _snapshotService = snapshots;
    }

    public async Task<DungeonRun> CreateAsync(Guid characterId, string dungeonDefinitionId, int seed, CancellationToken ct)
    {
        var dungeon = _dungeons.GetByKey(dungeonDefinitionId);
        var snapshot = await _snapshotService.CreateAsync(characterId, ct);

        // You can enforce access rules here if desired (key, recommended power, etc.)
        var rand = new Random(seed);

        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            DungeonDefinitionId = dungeonDefinitionId,
            Seed = seed,
            Status = DungeonRunStatus.Active,
            CurrentRoomIndex = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Initialize floor states (no event outcome yet)
        run.Rooms = CreateDungeonRooms(dungeon, rand);


        HydrateRooms(dungeon, run.Rooms, rand);
        // Apply base modifiers into run.ActiveModifiers (params copied for safety)
        //foreach (var m in dungeon.BaseModifiers)
        //{
        //    run.ActiveModifiers.Add(new RunModifier
        //    {
        //        ModifierDefinitionId = m.Id,
        //        Key = m.Key,
        //        Params = m.Params.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
        //        ExpiresAfterFloorIndex = null
        //    });
        //}

        // Optional: store initial snapshot-derived flags, etc.
        //run.Flags.PassedCheckpoint = false;

        return run;
    }

    private static List<RoomInstance> CreateDungeonRooms(DungeonDefinition dungeon, Random rand)
    {
        var rooms = new List<RoomInstance>();
        int totalRooms = rand.Next(dungeon.MinRooms, dungeon.MaxRooms + 1);
        var types = new RoomType[totalRooms];

        // Boss is always last
        types[totalRooms - 1] = RoomType.Boss;

        // Checkpoint (optional):
        int? checkpointIndex = null;
        if (dungeon.HasCheckpoint)
        {
            // There must be a checkpoint before the boss
            types[totalRooms - 2] = RoomType.Checkpoint;
        }

        // MiniBoss (optional): only if dungeon has miniboss encounters
        int? miniBossIndex = null;
        bool hasMiniboss = dungeon.Rooms.Any(r => r.Type == RoomType.MiniBoss);
        if (hasMiniboss)
        {
            // Prefer after checkpoint if it exists; otherwise somewhere before boss.
            int start = 2; // first two rooms can not be miniboss
            int end = totalRooms - (checkpointIndex.HasValue ? 3 : 2); // Can not appear in boss room, or endpoint before boss if there's one
            //if (start <= end)
            //{
            miniBossIndex = rand.Next(start, end + 1);

            //    // Avoid landing on checkpoint (if start==checkpoint+1 it's fine, but just in case)
            //    if (checkpointIndex.HasValue && miniBossIndex.Value == checkpointIndex.Value)
            //        miniBossIndex = null;
            //}

            if (miniBossIndex.HasValue)
                types[miniBossIndex.Value] = RoomType.MiniBoss;
        }
        // 2) Fill remaining slots with weighted Combat/Event
        // Tune these weights however you like.
        // TODO: Add Dungeon Events
        const int combatWeight = 80;
        const int eventWeight = 00;

        for (int i = 0; i < totalRooms; i++)
        {
            if (types[i] != default) continue; // already assigned (Boss/Checkpoint/MiniBoss)

            types[i] = RollWeighted(rand,
                (RoomType.Combat, combatWeight),
                (RoomType.Event, eventWeight));
        }

        // 3) Emit RoomInstance list
        for (int i = 0; i < totalRooms; i++)
        {
            rooms.Add(new RoomInstance
            {
                RoomIndex = i,
                Type = types[i],
                Status = RoomInstanceStatus.Pending
            });
        }

        return rooms;
    }

    private static void HydrateRooms(DungeonDefinition dungeon, List<RoomInstance> rooms, Random rand)
    {
        foreach (var room in rooms)
        {
            // For Checkpoint you might not need a template at all,
            // but if you *do* want checkpoint variants later, keep it consistent.
            if (room.Type == RoomType.Checkpoint)
                continue;

            var template = PickRoomVariantByType(dungeon, room.Type, rand);

            // If you want to record which variant got chosen (highly recommended),
            // add a field like room.TemplateIndex or room.TemplateHash later.
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

            case RoomType.Event:
                ResolveEventsFromTemplate(room, template, rand);
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

        // MiniBoss/Boss: take all authored monsters
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

    private static void ResolveEventsFromTemplate(RoomInstance room, RoomDefinition template, Random rand)
    {
        if (room.Type != RoomType.Event)
            throw new InvalidOperationException($"ResolveEventsFromTemplate called for non-event room type '{room.Type}'.");

        if (template.EncounterIds is null || template.EncounterIds.Count == 0)
            throw new InvalidOperationException("Event room template has no event ids.");

        // Reset event fields (important if you ever re-hydrate or reuse instances)
        room.EventOutcome = null;
        //room.Treasure = null;
        //room.Shrine = null;
        //room.Trap = null;

        // Sanitize candidates
        var candidates = new List<string>(template.EncounterIds.Count);
        foreach (var raw in template.EncounterIds)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            candidates.Add(raw.Trim());
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException("Event room template only contained empty event ids.");

        // Pick exactly one event id for this room
        var picked = candidates[rand.Next(candidates.Count)];

        // Expected formats:
        // "treasure:<variant>", "shrine:<variant>", "trap:<variant>"
        var colonIndex = picked.IndexOf(':');
        if (colonIndex <= 0 || colonIndex == picked.Length - 1)
            throw new InvalidOperationException(
                $"Invalid event id '{picked}'. Expected 'treasure:<id>' or 'shrine:<id>' or 'trap:<id>'.");

        var prefix = picked[..colonIndex].Trim().ToLowerInvariant();
        var variantId = picked[(colonIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(variantId))
            throw new InvalidOperationException($"Invalid event id '{picked}' (empty variant id).");

        //switch (prefix)
        //{
        //    case "treasure":
        //        room.EventOutcome = EventOutcomeType.Treasure;
        //        room.Treasure = new TreasureRoomInstance
        //        {
        //            // You’ll need a field like VariantId/TableId on this type.
        //            // If you don't have it, add it. Otherwise this data is lost.
        //            VariantId = variantId
        //        };
        //        break;

        //    case "shrine":
        //        room.EventOutcome = EventOutcomeType.Shrine;
        //        room.Shrine = new ShrineInstance
        //        {
        //            VariantId = variantId
        //        };
        //        break;

        //    case "trap":
        //        room.EventOutcome = EventOutcomeType.Trap;
        //        room.Trap = new TrapInstance
        //        {
        //            VariantId = variantId
        //        };
        //        break;

        //    default:
        //        throw new InvalidOperationException(
        //            $"Unknown event type prefix '{prefix}' in event id '{picked}'. Expected treasure/shrine/trap.");
        //}
    }

    private static RoomType RollWeighted(Random rand, params (RoomType type, int weight)[] options)
    {
        int total = 0;
        foreach (var (type, weight) in options)
            total += Math.Max(0, weight);

        if (total <= 0)
            return options[0].type;

        int roll = rand.Next(1, total + 1);
        int acc = 0;

        foreach (var (type, weight) in options)
        {
            acc += Math.Max(0, weight);
            if (roll <= acc)
                return type;
        }

        return options[^1].type;
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