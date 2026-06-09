using Application.Interfaces.Services.LL.Dungeons;
using Domain.Helpers.Constants;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Dungeons;

public sealed class DungeonRunService : IDungeonRunService
{
    private readonly IDungeonRunRepository _dungeonRuns;
    //private readonly IEncounterRepository _encounters;
    private readonly ICharacterSnapshotRepository _characterSnapshots;
    //private readonly IEncounterSelector _selector;
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;
    private readonly DungeonRunFactory _factory;
    private readonly IDungeonRunRewardClaimer _rewardClaimer;

    // Blessings are offered on shrine events; you’ll likely have a repository for these.
    //private readonly IReadOnlyList<Guid> _globalBlessingPool;

    // NOTE: You’ll need persistence (EF repo/unit of work). Kept out here for clarity.

    public DungeonRunService(
        IDungeonRunRepository dungeonRuns,
        //IEncounterRepository encounters,
        ICharacterSnapshotRepository characterSnapshots,
        //IEncounterSelector selector,
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        DungeonRunFactory factory,
        IDungeonRunRewardClaimer rewardClaimer
        //IDungeonRunStore runStore,
        /*IReadOnlyList<Guid> globalBlessingPool*/)
    {
        _dungeonRuns = dungeonRuns;
        //_encounters = encounters;
        _characterSnapshots = characterSnapshots;
        //_selector = selector;
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _factory = factory;
        _rewardClaimer = rewardClaimer;
        //_globalBlessingPool = globalBlessingPool;
    }

    public async Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, cancellationToken);
    }

    public async Task<bool> ClaimRewardsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var run = await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, cancellationToken);
        if (run == null || (run.Status != DungeonRunStatus.Completed && run.Status != DungeonRunStatus.Withdrawn))
            return false;

        await _rewardClaimer.ClaimAsync(run, cancellationToken);

        run.Status = DungeonRunStatus.RewardsClaimed;
        run.RewardsClaimedAt = DateTimeOffset.UtcNow;

        return await _dungeonRuns.DeleteDungeonRunAsync(run, cancellationToken);
    }

    public async Task<bool> DismissFailedRunAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var run = await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, cancellationToken);
        if (run == null || run.Status != DungeonRunStatus.Failed)
            return false;

        return await _dungeonRuns.DeleteDungeonRunAsync(run, cancellationToken);
    }

    public async Task<DungeonRun?> StartRunAsync(Guid characterId, string dungeonDefinitionId, CancellationToken ct)
    {
        var currentRun = await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, ct);
        if (currentRun != null) return null;

        // Seed: use cryptographic RNG or server-side monotonic; keep it server-owned.
        var seed = Random.Shared.Next(int.MinValue, int.MaxValue);

        var run = await _factory.CreateAsync(characterId, dungeonDefinitionId, seed, ct);

        await _dungeonRuns.CreateDungeonRunAsync(run, ct);
        return run;
    }

    public async Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid runId, string actionId, object? payload, CancellationToken ct)
    {
        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(runId, ct);
        if (run == null)
            return null;

        if (run.Status != DungeonRunStatus.Active)
            return null;

        var snapshot = await _characterSnapshots.GetSnapshotByCharacterIdAsync(run.CharacterId, ct);
        if (snapshot == null)
            return null;

        var room = GetCurrentRoom(run);
        if (room == null)
            return null;

        if (room.Status == RoomInstanceStatus.Completed)
            return null;

        actionId = actionId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(actionId))
            return null;

        switch (room.Type)
        {
            case RoomType.Combat:
            case RoomType.MiniBoss:
            case RoomType.Boss:
                return await ExecuteCombatRoomAction(run, snapshot, room, actionId, payload, ct);

            case RoomType.Event:
                return await ExecuteEventRoomAction(run, snapshot, room, actionId, payload, ct);

            case RoomType.Checkpoint:
                return await ExecuteCheckpointRoomAction(run, room, actionId, payload, ct);

            default:
                return null;
        }
    }

    private async Task<ExecuteDungeonActionResult?> ExecuteCombatRoomAction(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, string actionId, object? payload, CancellationToken ct)
    {
        switch (actionId.ToLowerInvariant())
        {
            case "fight":
                return await ResolveCombatRoom(run, snapshot, room, ct);

            case "leave":
                AbandonRun(run);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.RunAbandoned,
                    Message = "Dungeon run abandoned."
                };

            default:
                throw new InvalidOperationException(
                    $"Action '{actionId}' is not valid for room type '{room.Type}'.");
        }
    }

    private async Task<ExecuteDungeonActionResult?> ExecuteCheckpointRoomAction(DungeonRun run, RoomInstance room, string actionId, object? payload, CancellationToken ct)
    {
        switch (actionId.ToLowerInvariant())
        {
            case "continue":
                CompleteRoom(run, room);
                MoveToNextRoom(run);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.CheckpointResolved
                };

            case "leave":
            case "withdraw":
                run.Status = DungeonRunStatus.Withdrawn;
                run.CompletedAt = DateTimeOffset.UtcNow;
                room.Status = RoomInstanceStatus.Completed;

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = "Dungeon rewards secured."
                };

            default:
                return null;
        }
    }

    private async Task<ExecuteDungeonActionResult?> ExecuteEventRoomAction(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, string actionId, object? payload, CancellationToken ct)
    {
        switch (actionId.ToLowerInvariant())
        {
            case DungeonActionConstants.EventInspect:
                return await ResolveEventChoice(run, snapshot, room, DungeonActionConstants.EventInspect, ct);

            case DungeonActionConstants.EventAccept:
                return await ResolveEventChoice(run, snapshot, room, DungeonActionConstants.EventAccept, ct);

            case DungeonActionConstants.EventIgnore:
                return await ResolveEventChoice(run, snapshot, room, DungeonActionConstants.EventIgnore, ct);

            case DungeonActionConstants.Leave:
                AbandonRun(run);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved
                };

            default:
                throw new InvalidOperationException(
                    $"Action '{actionId}' is not valid for room type '{room.Type}'.");
        }
    }

    private async Task<ExecuteDungeonActionResult> ResolveCombatRoom(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, CancellationToken ct)
    {
        room.Status = RoomInstanceStatus.Active;

        var orchestrationRequest = new DungeonCombatOrchestrationRequest(
            DungeonRunId: run.Id,
            CharacterId: snapshot.CharacterId,
            CurrentRoomIndex: run.CurrentRoomIndex,
            EnemyCreatureKeys: room.EncounterIds);

        var orchestrationResult = await _orchestrationCoordinator.OrchestrateAsync(
            orchestrationRequest,
            ct);

        var outcomeRequest = new CombatOutcomeRequest(
            orchestrationRequest,
            orchestrationResult);

        var combatSession = await _outcomeCoordinator.ApplyAsync(
            outcomeRequest,
            ct);

        DungeonActionOutcome outcome;
        if (combatSession.CombatResult.Outcome == BattleOutcome.Victory)
        {
            CompleteRoom(run, room);
            MoveToNextRoom(run);
            
            outcome = run.Status == DungeonRunStatus.Completed
                ? DungeonActionOutcome.RunCompleted
                : DungeonActionOutcome.CombatVictory;
        }
        else
        {
            run.Status = DungeonRunStatus.Failed;
            room.Status = RoomInstanceStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;

            outcome = DungeonActionOutcome.CombatDefeat;
        }

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = outcome,
            CombatSession = combatSession
        };
    }

    private async Task<ExecuteDungeonActionResult> ResolveEventChoice(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, string optionId, CancellationToken ct)
    {
        room.Status = RoomInstanceStatus.Active;

        // Replace this with real event resolution logic.
        // Example:
        // - treasure -> loot
        // - shrine -> buff
        // - trap -> damage
        // - mystery -> weighted random outcome

        //switch (optionId)
        //{
        //    case DungeonActionConstants.EventInspect:
        //        ApplyEventInspectOutcome(run, room);
        //        break;

        //    case DungeonActionConstants.EventAccept:
        //        ApplyEventAcceptOutcome(run, room);
        //        break;

        //    case DungeonActionConstants.EventIgnore:
        //        ApplyEventIgnoreOutcome(run, room);
        //        break;

        //    default:
        //        throw new InvalidOperationException($"Unknown event option '{optionId}'.");
        //}

        CompleteRoom(run, room);
        MoveToNextRoom(run);

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved
        };
    }

    private void MoveToNextRoom(DungeonRun run)
    {
        var nextRoomIndex = run.CurrentRoomIndex + 1;

        if (run.Rooms == null || nextRoomIndex >= run.Rooms.Count)
        {
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt ??= DateTimeOffset.UtcNow;
            run.CurrentRoomIndex = Math.Max(0, (run.Rooms?.Count ?? 1) - 1);
            return;
        }

        run.CurrentRoomIndex = nextRoomIndex;
    }

    private void AbandonRun(DungeonRun run)
    {
        run.Status = DungeonRunStatus.Failed;
    }

    //public async Task<DungeonRun> WithdrawAsync(Guid runId, CancellationToken ct)
    //{
    //    var run = await _runStore.GetAsync(runId, ct);
    //    if (run.Status != DungeonRunStatus.Active) return run;

    //    var floor = GetCurrentFloor(run);
    //    if (floor.Type != RoomType.Checkpoint)
    //        throw new InvalidOperationException("Withdraw is only allowed at checkpoint.");

    //    run.Status = DungeonRunStatus.Withdrawn;
    //    run.CompletedAt = DateTimeOffset.UtcNow;

    //    // Bank rewards here (grant inventory, etc.) via your reward service.
    //    // Keep it out of this class if you want clean layering.

    //    await _runStore.UpdateAsync(run, ct);
    //    return run;
    //}

    //public async Task<DungeonRun> SelectTreasureOptionAsync(Guid runId, int optionIndex, CancellationToken ct)
    //{
    //    var run = await _runStore.GetAsync(runId, ct);
    //    if (run.Status != DungeonRunStatus.Active) return run;

    //    var floor = GetCurrentFloor(run);
    //    if (floor.Type != RoomType.Event || floor.Treasure is null)
    //        throw new InvalidOperationException("No treasure to select on this floor.");

    //    if (floor.Treasure.Resolved) return run;

    //    if (optionIndex < 0 || optionIndex >= floor.Treasure.Options.Count)
    //        throw new ArgumentOutOfRangeException(nameof(optionIndex));

    //    floor.Treasure.SelectedOptionIndex = optionIndex;
    //    floor.Treasure.Resolved = true;

    //    // Apply effects/rewards of selected option
    //    ApplyTreasureSelection(run, floor.Treasure.Options[optionIndex]);

    //    // After resolving treasure, complete floor and advance.
    //    CompleteFloor(run, floor);
    //    MoveToNextFloor(run);

    //    await _runStore.UpdateAsync(run, ct);
    //    return run;
    //}

    //public async Task<DungeonRun> SelectShrineBlessingAsync(Guid runId, Guid blessingId, CancellationToken ct)
    //{
    //    var run = await _runStore.GetAsync(runId, ct);
    //    if (run.Status != DungeonRunStatus.Active) return run;

    //    var floor = GetCurrentFloor(run);
    //    if (floor.Type != RoomType.Event || floor.Shrine is null)
    //        throw new InvalidOperationException("No shrine to select on this floor.");

    //    if (floor.Shrine.Resolved) return run;

    //    if (!floor.Shrine.OfferedBlessingIds.Contains(blessingId))
    //        throw new InvalidOperationException("Blessing not offered.");

    //    floor.Shrine.SelectedBlessingId = blessingId;
    //    floor.Shrine.Resolved = true;

    //    // Apply blessing (store as RunBlessing; your combat engine reads it)
    //    run.AppliedBlessings.Add(new RunBlessing
    //    {
    //        BlessingDefinitionId = blessingId,
    //        Key = $"blessing:{blessingId}",
    //        Params = new Dictionary<string, string>()
    //    });

    //    CompleteFloor(run, floor);
    //    MoveToNextFloor(run);

    //    await _runStore.UpdateAsync(run, ct);
    //    return run;
    //}

    //public Task<DungeonRun> SwapCheckpointEssenceAsync(Guid runId, Guid removeEssenceId, Guid addEssenceId, CancellationToken ct)
    //    => throw new NotImplementedException("Implement after you wire your essence loadout system.");

    //// -------------------- Resolution helpers --------------------

    //private async Task ResolveEventFloor(
    //    DungeonRun run,
    //    DungeonDefinition dungeon,
    //    CharacterDungeonSnapshot snapshot,
    //    RoomInstance floor,
    //    DeterministicRng rng,
    //    CancellationToken ct)
    //{
    //    floor.Status = RoomInstanceStatus.Active;

    //    // If we haven't rolled the event outcome yet, roll now.
    //    if (floor.EventOutcome is null)
    //    {
    //        floor.EventOutcome = DungeonEventGenerator.RollEvent(dungeon.EventTable, rng);
    //    }

    //    switch (floor.EventOutcome.Value)
    //    {
    //        case EventOutcomeType.ExtraCombat:
    //            // Treat as a single hard pack encounter (generated on demand)
    //            await ResolveSingleExtraCombatEncounter(run, dungeon, snapshot, floor, rng, ct);
    //            break;

    //        case EventOutcomeType.TreasureRoom:
    //            // Generate options and WAIT for player to choose (asynchronous in gameplay terms)
    //            floor.Treasure ??= DungeonEventGenerator.GenerateTreasure(snapshot, rng);
    //            // Do not auto-advance; player must pick.
    //            break;

    //        case EventOutcomeType.Shrine:
    //            floor.Shrine ??= DungeonEventGenerator.GenerateShrine(rng, _globalBlessingPool);
    //            // Wait for player selection
    //            break;

    //        case EventOutcomeType.Trap:
    //            floor.Trap ??= DungeonEventGenerator.GenerateTrap(rng);
    //            ApplyTrap(run, floor.Trap);
    //            floor.Trap.Resolved = true;
    //            CompleteFloor(run, floor);
    //            MoveToNextFloor(run);
    //            break;

    //        default:
    //            throw new InvalidOperationException($"Unhandled event outcome: {floor.EventOutcome.Value}");
    //    }
    //}

    //private async Task ResolveCombatFloor(DungeonRun run, CharacterSnapshot characterSnapshot, RoomInstance floor, DeterministicRng rng, CancellationToken ct)
    //{
    //    floor.Status = RoomInstanceStatus.Active;

    //    // Generate encounters for this floor if not generated.
    //    if (floor.EncounterIds.Count == 0)
    //    {
    //        var count = rng.NextInt(1, 5); // a random count between 1 and 4

    //        var pool = await _encounters.GetPackEncountersForDungeonAsync(floor.EncounterIds, ct);
    //        var selected = _selector.SelectPackEncounters(count, pool, rng);

    //        floor.EncounterIds.AddRange(selected);
    //    }

    //    await ResolveEncounterOnFloor(run, characterSnapshot, floor, ct);
    //}

    //private async Task ResolveMiniBossFloor(
    //    DungeonRun run,
    //    DungeonDefinition dungeon,
    //    CharacterDungeonSnapshot snapshot,
    //    RoomInstance floor,
    //    DeterministicRng rng,
    //    CancellationToken ct)
    //{
    //    floor.Status = RoomInstanceStatus.Active;

    //    if (floor.EncounterIds.Count == 0)
    //    {
    //        var id = _selector.SelectMiniBoss(dungeon, snapshot, rng);
    //        floor.EncounterIds.Add(id);
    //        run.CurrentEncounterIndex = 0;
    //    }

    //    await ResolveEncounterOnFloor(run, floor, ct);
    //}

    //private async Task ResolveBossFloor(
    //    DungeonRun run,
    //    DungeonDefinition dungeon,
    //    CharacterDungeonSnapshot snapshot,
    //    RoomInstance floor,
    //    DeterministicRng rng,
    //    CancellationToken ct)
    //{
    //    floor.Status = RoomInstanceStatus.Active;

    //    if (floor.EncounterIds.Count == 0)
    //    {
    //        var id = _selector.SelectBoss(dungeon, snapshot, rng);
    //        floor.EncounterIds.Add(id);
    //        run.CurrentEncounterIndex = 0;
    //    }

    //    await ResolveEncounterOnFloor(run, floor, ct);
    //}

    //private async Task ResolveSingleExtraCombatEncounter(
    //    DungeonRun run,
    //    DungeonDefinition dungeon,
    //    CharacterDungeonSnapshot snapshot,
    //    RoomInstance floor,
    //    DeterministicRng rng,
    //    CancellationToken ct)
    //{
    //    if (floor.EncounterIds.Count == 0)
    //    {
    //        var pool = await _encounters.GetPackEncountersForDungeonAsync(dungeon.Id, ct);
    //        floor.EncounterIds.Add(rng.ChooseOne(pool));
    //        run.CurrentEncounterIndex = 0;
    //    }

    //    await ResolveEncounterOnFloor(run, floor, ct);
    //}

    //private async Task ResolveEncounterOnFloor(DungeonRun run, CharacterSnapshot characterSnapshot, RoomInstance floor, CancellationToken ct)
    //{

    //    var encounter = await _encounters.GetEncountersAsync(floor.EncounterIds, ct);
    //    var enemyCharacters = await _entityService.GetEntitiesByIdsForCombatAsync(entityIds, cancellationToken);

    //    //var modifiers = BuildCombatModifierParams(run, encounter);

    //    var request = new DungeonCombatRequest(
    //        run.CharacterId,
    //        encounter.MonsterIds
    //    );

    //    CombatResult result = await _combat.InstantiateAndRunCombat(request, ct);

    //    if (result.Outcome != BattleOutcome.Victory)
    //    {
    //        run.Status = DungeonRunStatus.Failed;
    //        run.CompletedAt = DateTimeOffset.UtcNow;

    //        // Optional: partial rewards on fail (your call)
    //        // run.PendingRewards.Add(...)

    //        return;
    //    }

    //    // Win: collect loot into pending rewards
    //    foreach (var grant in result.Loot)
    //    {
    //        run.PendingRewards.Add(new RunReward
    //        {
    //            ItemId = grant.ItemInstanceId.ToString(),
    //            Quantity = grant.Quantity,
    //            Source = floor.Type == RoomType.Boss ? "boss" : floor.Type == RoomType.MiniBoss ? "mini-boss" : $"floor:{floor.RoomIndex}"
    //        });
    //    }
    //}

    //// -------------------- Modifier application / expiry --------------------

    //private void ApplyFloorEntryModifiersIfNeeded(DungeonRun run, DungeonDefinition dungeon, RoomInstance floor)
    //{
    //    // Apply floor-specific modifiers exactly once, on first activation
    //    if (floor.Status != RoomInstanceStatus.Pending) return;

    //    var floorDef = dungeon.Rooms.First(f => f.Index == floor.FloorIndex);
    //    foreach (var m in floorDef.Modifiers)
    //    {
    //        run.ActiveModifiers.Add(new RunModifier
    //        {
    //            ModifierDefinitionId = m.Id,
    //            Key = m.Key,
    //            Params = m.Params.ToDictionary(k => k.Key, v => v.Value),
    //            ExpiresAfterFloorIndex = null
    //        });
    //    }
    //}

    //private void ExpireModifiers(DungeonRun run)
    //{
    //    // Example expiry logic if you set ExpiresAfterFloorIndex
    //    var currentFloor = run.CurrentFloorIndex;
    //    run.ActiveModifiers.RemoveAll(m => m.ExpiresAfterFloorIndex.HasValue && m.ExpiresAfterFloorIndex.Value < currentFloor);
    //}

    //private static IReadOnlyList<DungeonEffectParam> BuildCombatModifierParams(DungeonRun run, EncounterDefinition encounter)
    //{
    //    var list = new List<DungeonEffectParam>();

    //    // Run-wide modifiers
    //    foreach (var m in run.ActiveModifiers)
    //        list.Add(new DungeonEffectParam(m.Key, m.Params));

    //    // Blessings
    //    foreach (var b in run.AppliedBlessings)
    //        list.Add(new DungeonEffectParam(b.Key, b.Params));

    //    // Encounter-specific modifiers
    //    foreach (var em in encounter.Modifiers)
    //        list.Add(new DungeonEffectParam(em.Key, em.Params));

    //    return list;
    //}

    //private static void ApplyTreasureSelection(DungeonRun run, TreasureOptionInstance opt)
    //{
    //    switch (opt.Type)
    //    {
    //        case TreasureOptionType.SafeLoot:
    //            // Example: add a placeholder reward roll token; your reward service can interpret
    //            run.PendingRewards.Add(new RunReward { ItemId = "treasure_roll_token", Quantity = 1, Source = "treasure" });
    //            break;

    //        case TreasureOptionType.CursedChest:
    //            run.Flags.OpenedCursedChest = true;
    //            run.PendingRewards.Add(new RunReward { ItemId = "treasure_roll_token", Quantity = 2, Source = "treasure" });
    //            run.ActiveModifiers.Add(new RunModifier
    //            {
    //                ModifierDefinitionId = Guid.Empty,
    //                Key = opt.Params.TryGetValue("applyModifierKey", out var key) ? key : "cursed_chest_debuff",
    //                Params = new Dictionary<string, string> { ["severity"] = "1" }
    //            });
    //            break;

    //        case TreasureOptionType.TradeHealthForLoot:
    //            run.PendingRewards.Add(new RunReward { ItemId = "treasure_roll_token", Quantity = 2, Source = "treasure" });
    //            // The “health loss” should be applied in combat state / next fight via modifier
    //            run.ActiveModifiers.Add(new RunModifier
    //            {
    //                ModifierDefinitionId = Guid.Empty,
    //                Key = "max_health_reduced_pct",
    //                Params = new Dictionary<string, string> { ["pct"] = opt.Params.GetValueOrDefault("maxHealthLossPct", "10") }
    //            });
    //            break;

    //        default:
    //            // Keep forward-compatible.
    //            run.PendingRewards.Add(new RunReward { ItemId = "treasure_roll_token", Quantity = 1, Source = "treasure" });
    //            break;
    //    }
    //}

    //private static void ApplyTrap(DungeonRun run, TrapInstance trap)
    //{
    //    // Convert trap into run modifier(s)
    //    run.ActiveModifiers.Add(new RunModifier
    //    {
    //        ModifierDefinitionId = Guid.Empty,
    //        Key = $"trap:{trap.TrapKey}",
    //        Params = trap.Params.ToDictionary(k => k.Key, v => v.Value),
    //        ExpiresAfterFloorIndex = run.CurrentFloorIndex + 1 // example: lasts 1 floor
    //    });
    //}

    //// -------------------- Floor navigation --------------------

    private static RoomInstance GetCurrentRoom(DungeonRun run)
    {
        var floor = run.Rooms.FirstOrDefault(f => f.RoomIndex == run.CurrentRoomIndex);
        if (floor is null) throw new InvalidOperationException("Run floor state missing.");
        return floor;
    }

    private static void CompleteRoom(DungeonRun run, RoomInstance room)
    {
        room.Status = RoomInstanceStatus.Completed;
    }

    private static void MoveToNextFloor(DungeonRun run)
    {
        var nextIndex = run.CurrentRoomIndex + 1;

        // Find next floor state
        var next = run.Rooms.FirstOrDefault(f => f.RoomIndex == nextIndex);
        if (next is null)
        {
            // End of dungeon
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            return;
        }

        run.CurrentRoomIndex = nextIndex;
        next.Status = RoomInstanceStatus.Active;
    }
}

public interface IDungeonRunStore
{
    Task InsertAsync(DungeonRun run, CancellationToken ct);
    Task<DungeonRun> GetAsync(Guid runId, CancellationToken ct);
    Task UpdateAsync(DungeonRun run, CancellationToken ct);
}
