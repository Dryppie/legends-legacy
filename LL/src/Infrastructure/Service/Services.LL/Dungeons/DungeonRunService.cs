using Application.Interfaces.Services.LL.Dungeons;
using Domain.Helpers.Constants;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Guilds.Missions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using System.Text.Json;

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
    private readonly IDungeonCompletionRewardApplier _completionRewardApplier;
    private readonly IDungeonDefinitions _dungeons;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryRepository _inventory;
    private readonly IDungeonPressureService _pressure;
    private readonly IDungeonRouteService _routes;
    private readonly IDungeonBoonService _boons;
    private readonly IDungeonCheckpointService _checkpoints;
    private readonly IDungeonEventChoiceService _events;
    private readonly IDungeonMasteryService _mastery;
    private readonly IDungeonBossModifierService _bossModifiers;
    private readonly IDungeonEncounterModifierService _encounterModifiers;
    private readonly IGuildMissionService _guildMissionService;

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
        IDungeonRunRewardClaimer rewardClaimer,
        IDungeonCompletionRewardApplier completionRewardApplier,
        IDungeonDefinitions dungeons,
        IItemBaseRepository itemBases,
        IInventoryRepository inventory,
        IDungeonPressureService pressure,
        IDungeonRouteService routes,
        IDungeonBoonService boons,
        IDungeonCheckpointService checkpoints,
        IDungeonEventChoiceService events,
        IDungeonMasteryService mastery,
        IDungeonBossModifierService bossModifiers,
        IDungeonEncounterModifierService encounterModifiers,
        IGuildMissionService guildMissionService
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
        _completionRewardApplier = completionRewardApplier;
        _dungeons = dungeons;
        _itemBases = itemBases;
        _inventory = inventory;
        _pressure = pressure;
        _routes = routes;
        _boons = boons;
        _checkpoints = checkpoints;
        _events = events;
        _mastery = mastery;
        _bossModifiers = bossModifiers;
        _encounterModifiers = encounterModifiers;
        _guildMissionService = guildMissionService;
        //_globalBlessingPool = globalBlessingPool;
    }

    public async Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var run = await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, cancellationToken);
        if (run is not null)
        {
            EnsureRunState(run);
        }

        return run;
    }

    public async Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken)
    {
        return await _dungeonRuns.GetCompletionRecordsAsync(
            characterId,
            dungeonDefinitionIds,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken)
    {
        return await _dungeonRuns.GetCompletionLeaderboardAsync(
            dungeonDefinitionIds,
            cancellationToken);
    }

    public async Task<ClaimDungeonRewardsResult?> ClaimRewardsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var run = await _dungeonRuns.GetDungeonRunByCharacterIdAsync(characterId, cancellationToken);
        if (run == null || (run.Status != DungeonRunStatus.Completed && run.Status != DungeonRunStatus.Withdrawn))
            return null;

        var claimedLoot = await _rewardClaimer.ClaimAsync(run, cancellationToken);
        var result = new ClaimDungeonRewardsResult
        {
            ClaimedLoot = claimedLoot,
            WasCompleted = run.Status == DungeonRunStatus.Completed,
            DungeonDefinitionId = run.DungeonDefinitionId,
            CompletedWithoutDefeat = run.DeathsDuringRun == 0,
            CompletedWithoutCheckpointRetreat = !run.UsedCheckpointRetreat,
            DefeatedBossKeys = run.Rooms
                .Where(room => room.Type == RoomType.Boss && room.Status == RoomInstanceStatus.Completed)
                .SelectMany(room => room.EncounterIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        run.Status = DungeonRunStatus.RewardsClaimed;
        run.RewardsClaimedAt = DateTimeOffset.UtcNow;

        var deleted = await _dungeonRuns.DeleteDungeonRunAsync(run, cancellationToken);
        return deleted ? result : null;
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

        var dungeonDefinition = _dungeons.GetByKey(dungeonDefinitionId);
        await ConsumeEntryCostsAsync(characterId, dungeonDefinition, ct);

        // Seed: use cryptographic RNG or server-side monotonic; keep it server-owned.
        var seed = Random.Shared.Next(int.MinValue, int.MaxValue);

        var run = await _factory.CreateAsync(characterId, dungeonDefinitionId, seed, ct);
        await _mastery.ApplyStartBonusesAsync(run, ct);

        await _dungeonRuns.CreateDungeonRunAsync(run, ct);
        return run;
    }

    private async Task ConsumeEntryCostsAsync(
        Guid characterId,
        DungeonDefinition dungeonDefinition,
        CancellationToken cancellationToken)
    {
        var consumedCosts = dungeonDefinition.EntryCosts
            .Where(x => x.Amount > 0 && !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(cost => cost.Amount), StringComparer.OrdinalIgnoreCase);

        if (consumedCosts.Count == 0)
        {
            return;
        }

        var removed = await _inventory.TryRemoveItemsByBaseIdAsync(
            characterId,
            consumedCosts,
            cancellationToken);

        if (!removed)
        {
            throw new InvalidOperationException("Dungeon entry costs could not be consumed.");
        }
    }

    public async Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid runId, string actionId, object? payload, CancellationToken ct)
    {
        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(runId, ct);
        if (run == null)
            return null;

        if (run.Status != DungeonRunStatus.Active)
            return null;

        actionId = actionId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(actionId))
            return null;

        EnsureRunState(run);

        if (run.State.CurrentBoonChoices.Count > 0)
        {
            if (actionId.Equals(DungeonActionConstants.ChooseBoon, StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteChooseBoonAction(run, payload, ct);
            }

            if (actionId.Equals(DungeonActionConstants.Leave, StringComparison.OrdinalIgnoreCase))
            {
                AbandonRun(run);
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.RunAbandoned,
                    Message = "Dungeon run abandoned."
                };
            }

            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.None,
                Message = "Choose a boon before resolving the next room."
            };
        }

        if (actionId.Equals(DungeonActionConstants.ChooseBoon, StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteChooseBoonAction(run, payload, ct);
        }

        if (run.State.CurrentRouteOptions.Count > 0)
        {
            if (actionId.Equals(DungeonActionConstants.ChooseRoute, StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteChooseRouteAction(run, payload);
            }

            if (actionId.Equals(DungeonActionConstants.Leave, StringComparison.OrdinalIgnoreCase))
            {
                AbandonRun(run);
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.RunAbandoned,
                    Message = "Dungeon run abandoned."
                };
            }

            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.None,
                Message = "Choose a route before resolving the next room."
            };
        }

        var room = GetCurrentRoom(run);
        if (room == null)
            return null;

        if (room.Status == RoomInstanceStatus.Completed)
            return null;

        switch (room.Type)
        {
            case RoomType.Combat:
            case RoomType.MiniBoss:
            case RoomType.Boss:
                var snapshot = await _characterSnapshots.GetSnapshotByCharacterIdAsync(run.CharacterId, ct);
                if (snapshot == null)
                    return null;

                return await ExecuteCombatRoomAction(run, snapshot, room, actionId, payload, ct);

            case RoomType.Event:
                return await ExecuteEventRoomAction(run, room, actionId, payload, ct);

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
        _checkpoints.EnsureChoices(run);

        switch (actionId.ToLowerInvariant())
        {
            case DungeonActionConstants.CheckpointChoice:
                return await ApplyCheckpointChoiceAsync(run, room, payload, ct);

            case "continue":
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

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
                run.UsedCheckpointRetreat = true;
                run.CompletedAt = DateTimeOffset.UtcNow;
                room.Status = RoomInstanceStatus.Completed;
                run.State.SecuredLoot = CreateLootBagFromRun(run);
                run.State.UnsecuredLoot = new DungeonLootBag();
                ClearDecisionState(run);

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

    private async Task<ExecuteDungeonActionResult?> ExecuteEventRoomAction(DungeonRun run, RoomInstance room, string actionId, object? payload, CancellationToken ct)
    {
        var dungeon = _dungeons.GetByKey(run.DungeonDefinitionId);
        room.EventOutcome ??= RollEventOutcome(run, room);

        switch (actionId.ToLowerInvariant())
        {
            case DungeonActionConstants.EventChoice:
                return await ApplyEventChoiceAsync(run, dungeon, room, payload, ct);

            case DungeonActionConstants.EventInspect:
                room.Status = RoomInstanceStatus.Active;
                _events.EnsureChoices(run, dungeon.Id, room.EventOutcome.Value);
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.EventResolved,
                    Message = GetEventInspectMessage(room.EventOutcome.Value)
                };

            case DungeonActionConstants.EventAccept:
                if (_events.EnsureChoices(run, dungeon.Id, room.EventOutcome.Value).Count > 0)
                {
                    room.Status = RoomInstanceStatus.Active;
                    return new ExecuteDungeonActionResult
                    {
                        Run = run,
                        Outcome = DungeonActionOutcome.EventResolved,
                        Message = GetEventInspectMessage(room.EventOutcome.Value)
                    };
                }

                return await AcceptEventAsync(run, dungeon, room, ct);

            case DungeonActionConstants.EventIgnore:
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved,
                    Message = "You leave the event untouched and move deeper into the dungeon."
                };

            case DungeonActionConstants.Leave:
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

    private async Task<ExecuteDungeonActionResult> ResolveCombatRoom(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, CancellationToken ct)
    {
        room.Status = RoomInstanceStatus.Active;
        var dungeon = _dungeons.GetByKey(run.DungeonDefinitionId);
        run.State.CurrentBossModifiers = _bossModifiers.GetActiveBossModifiers(run, dungeon, room).ToList();
        var enemyAttributeModifiers = room.Type == RoomType.Boss
            ? _bossModifiers.GetActiveBossAttributeModifiers(run, dungeon, room)
            : _encounterModifiers.GetActiveEnemyAttributeModifiers(run, dungeon, room);

        var orchestrationRequest = new DungeonCombatOrchestrationRequest(
            DungeonRunId: run.Id,
            CharacterId: snapshot.CharacterId,
            CharacterSnapshot: snapshot,
            CurrentRoomIndex: run.CurrentRoomIndex,
            EnemyCreatureKeys: room.EncounterIds,
            RunAttributeModifiers: _boons.GetActiveAttributeModifiers(run),
            RunAbilityModifiers: _boons.GetActiveAbilityModifiers(run),
            EnemyAttributeModifiers: enemyAttributeModifiers);

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
            ApplyRoomCompletionPressure(run, room);
            CompleteRoom(run, room);
            if (run.State.CurrentBoonChoices.Count == 0)
            {
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);
            }
            else
            {
                await RecordDungeonProgressContributionAsync(run, room, ct);
            }
            
            outcome = run.Status == DungeonRunStatus.Completed
                ? DungeonActionOutcome.RunCompleted
                : DungeonActionOutcome.CombatVictory;
        }
        else
        {
            run.Status = DungeonRunStatus.Failed;
            run.DeathsDuringRun++;
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

    private async Task<ExecuteDungeonActionResult> AcceptEventAsync(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room,
        CancellationToken ct)
    {
        room.Status = RoomInstanceStatus.Active;

        switch (room.EventOutcome ?? EventOutcomeType.TreasureRoom)
        {
            case EventOutcomeType.ExtraCombat:
                room.Type = RoomType.Combat;
                room.EncounterIds = ResolveExtraCombatEncounters(run, dungeon, room);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.EventResolved,
                    Message = "The disturbance draws enemies into your path."
                };

            case EventOutcomeType.TreasureRoom:
                await AddTreasureEventRewardsAsync(run, dungeon, room, ct);
                _pressure.ApplyPressureDelta(run, 4);
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved,
                    Message = "You secure the hidden cache."
                };

            case EventOutcomeType.Shrine:
                run.PendingSoulstones += Math.Max(1, dungeon.Tier);
                run.PendingExperience += Math.Max(10, dungeon.Tier * 15);
                run.State.UnsecuredLoot.Soulstones += Math.Max(1, dungeon.Tier);
                run.State.UnsecuredLoot.Experience += Math.Max(10, dungeon.Tier * 15);
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved,
                    Message = "The shrine answers with a quiet pulse of power."
                };

            case EventOutcomeType.Trap:
                var lostCinders = Math.Min(run.PendingCinders, Math.Max(10, dungeon.Tier * 20));
                run.PendingCinders -= lostCinders;
                run.State.UnsecuredLoot.Cinders = Math.Max(0, run.State.UnsecuredLoot.Cinders - lostCinders);
                _pressure.ApplyPressureDelta(run, 10);
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.EventResolved,
                    Message = lostCinders > 0
                        ? $"The trap snaps shut. You lose {lostCinders} pending Cinders."
                        : "The trap snaps shut, but you had no pending Cinders to lose."
                };

            default:
                throw new InvalidOperationException($"Unhandled dungeon event outcome '{room.EventOutcome}'.");
        }
    }

    private ExecuteDungeonActionResult? ExecuteChooseRouteAction(DungeonRun run, object? payload)
    {
        if (!TryGetPayloadString(payload, "routeOptionId", out var routeOptionId))
        {
            return null;
        }

        var route = ChooseRoute(run, routeOptionId);
        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.None,
            Message = $"{route.DisplayName} chosen. {FormatPressureDelta(route.PressureDelta)}"
        };
    }

    private async Task<ExecuteDungeonActionResult?> ExecuteChooseBoonAction(
        DungeonRun run,
        object? payload,
        CancellationToken ct)
    {
        if (!TryGetPayloadString(payload, "boonId", out var boonId))
        {
            return null;
        }

        _boons.ChooseBoon(run, boonId);

        var shouldCompleteRoom = run.State.Flags.Remove("pending_boon_completes_room");
        var shouldAdvanceRoom = run.State.Flags.Remove("pending_boon_advances_room");
        var completedRoomType = GetCurrentRoom(run)?.Type;
        if (shouldCompleteRoom)
        {
            var room = GetCurrentRoom(run);
            CompleteRoom(run, room);
            MoveToNextRoom(run);
            await RecordDungeonProgressContributionAsync(run, room, ct);
            await ApplyCompletionRewardsIfNeeded(run, ct);
        }
        else if (shouldAdvanceRoom)
        {
            var room = GetCurrentRoom(run);
            if (room?.Status == RoomInstanceStatus.Completed)
            {
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);
            }
        }

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = shouldCompleteRoom
                ? completedRoomType == RoomType.Checkpoint
                    ? DungeonActionOutcome.CheckpointResolved
                    : DungeonActionOutcome.EventResolved
                : shouldAdvanceRoom
                    ? DungeonActionOutcome.CombatVictory
                    : DungeonActionOutcome.CheckpointResolved,
            Message = "Temporary dungeon boon chosen."
        };
    }

    private async Task<ExecuteDungeonActionResult?> ApplyCheckpointChoiceAsync(
        DungeonRun run,
        RoomInstance room,
        object? payload,
        CancellationToken ct)
    {
        if (!TryGetPayloadString(payload, "choice", out var choiceId))
        {
            return null;
        }

        DungeonCheckpointChoiceResult result;
        try
        {
            result = _checkpoints.ApplyChoice(run, room, choiceId);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        switch (result.Outcome)
        {
            case DungeonCheckpointChoiceOutcome.Withdraw:
                run.UsedCheckpointRetreat = true;
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = "Dungeon rewards secured."
                };

            case DungeonCheckpointChoiceOutcome.Focus:
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = "Choose one temporary boon for the rest of this run."
                };

            case DungeonCheckpointChoiceOutcome.PushDeeper:
                CompleteRoom(run, room);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.CheckpointResolved,
                    Message = "You push deeper. Rewards rise, but so does the danger."
                };

            case DungeonCheckpointChoiceOutcome.Rest:
                run.UsedCheckpointRetreat = true;
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = "You rest and recover, losing a little unsecured loot."
                };

            default:
                return null;
        }
    }

    private async Task<ExecuteDungeonActionResult?> ApplyEventChoiceAsync(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room,
        object? payload,
        CancellationToken ct)
    {
        if (!TryGetPayloadString(payload, "choiceId", out var choiceId))
        {
            return null;
        }

        room.Status = RoomInstanceStatus.Active;
        var eventOutcome = room.EventOutcome ?? EventOutcomeType.TreasureRoom;
        _events.EnsureChoices(run, dungeon.Id, eventOutcome);
        DungeonEventChoiceOption choice;
        try
        {
            choice = _events.ApplyChoiceState(run, choiceId);
        }
        catch (InvalidOperationException)
        {
            run.State.CurrentEventChoices.Clear();
            _events.EnsureChoices(run, dungeon.Id, eventOutcome);

            try
            {
                choice = _events.ApplyChoiceState(run, choiceId);
            }
            catch (InvalidOperationException)
            {
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.None,
                    Message = "That event choice is no longer available."
                };
            }
        }

        if (choice.AmbushChancePercent > 0 &&
            run.State.Flags.GetValueOrDefault("event_ambush_triggered") > 0)
        {
            run.State.Flags.Remove("event_ambush_triggered");
            room.Type = RoomType.Combat;
            room.EncounterIds = ResolveExtraCombatEncounters(run, dungeon, room);
            run.State.CurrentEventChoices.Clear();

            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.EventResolved,
                Message = "The event erupts into an ambush."
            };
        }

        if (choice.GrantsLoot)
        {
            await AddChoiceLootAsync(run, dungeon, room, choice.Id, ct);
        }

        if (choice.Id == "sacrifice_loot")
        {
            ReduceUnsecuredLoot(run, 0.15m);
        }

        if (choice.Id == "engage_patrol")
        {
            room.Type = RoomType.Combat;
            room.EncounterIds = ResolveExtraCombatEncounters(run, dungeon, room);
            run.State.CurrentEventChoices.Clear();

            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.EventResolved,
                Message = "The route turns into a fight."
            };
        }

        if (choice.GrantsBoonChoice)
        {
            run.State.CurrentEventChoices.Clear();
            if (_boons.GenerateBoonChoices(run).Count > 0)
            {
                AddFlag(run, "pending_boon_completes_room", 1);
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.EventResolved,
                    Message = "Choose one temporary boon from the event."
                };
            }
        }

        run.State.CurrentEventChoices.Clear();
        CompleteRoom(run, room);
        MoveToNextRoom(run);
        await RecordDungeonProgressContributionAsync(run, room, ct);
        if (choice.RevealsHiddenRoute)
        {
            AddHiddenRouteOption(run);
        }

        await ApplyCompletionRewardsIfNeeded(run, ct);

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = run.Status == DungeonRunStatus.Completed
                ? DungeonActionOutcome.RunCompleted
                : DungeonActionOutcome.EventResolved,
            Message = choice.Description
        };
    }

    private static EventOutcomeType RollEventOutcome(DungeonRun run, RoomInstance room)
    {
        var eventTable = new EventTableDefinition();
        var totalWeight = eventTable.Outcomes.Sum(x => Math.Max(0, x.Weight));
        if (totalWeight <= 0)
            return EventOutcomeType.TreasureRoom;

        var rand = new Random(CreateRoomSeed(run.Seed, room.RoomIndex, 17));
        var roll = rand.Next(1, totalWeight + 1);
        var accumulated = 0;

        foreach (var outcome in eventTable.Outcomes)
        {
            accumulated += Math.Max(0, outcome.Weight);
            if (roll <= accumulated)
                return outcome.Type;
        }

        return eventTable.Outcomes[^1].Type;
    }

    private static string GetEventInspectMessage(EventOutcomeType outcome) =>
        outcome switch
        {
            EventOutcomeType.ExtraCombat => "Tracks and echoes suggest an enemy patrol is close.",
            EventOutcomeType.TreasureRoom => "You find the edge of a hidden cache.",
            EventOutcomeType.Shrine => "A worn shrine hums with stored power.",
            EventOutcomeType.Trap => "The room is tense with pressure plates and old wire.",
            _ => "Something waits in the room."
        };

    private async Task AddTreasureEventRewardsAsync(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room,
        CancellationToken cancellationToken)
    {
        var rewardMultiplier = Math.Max(100, run.State.RewardMultiplierPercent) / 100m;
        var cinders = (int)Math.Ceiling(Math.Max(20, dungeon.Tier * 35) * rewardMultiplier);
        var soulstones = Math.Max(1, (int)dungeon.Grade);

        run.PendingCinders += cinders;
        run.PendingSoulstones += soulstones;
        run.State.UnsecuredLoot.Cinders += cinders;
        run.State.UnsecuredLoot.Soulstones += soulstones;

        var itemId = DungeonRewardCatalog.GetMonsterCoreRewardItemIds(dungeon.Grade).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemId], cancellationToken);
        if (!itemBases.TryGetValue(itemId, out var itemBase))
            return;

        await _dungeonRuns.AddPendingRewardAsync(run, new RunReward
        {
            ItemId = itemBase.Id,
            Name = itemBase.Name,
            ItemType = itemBase.ItemType,
            Quantity = Math.Max(1, (int)dungeon.Grade),
            Source = $"event:treasure:room:{room.RoomIndex + 1}"
        }, cancellationToken);
        run.State.UnsecuredLoot.Items[itemBase.Id] =
            run.State.UnsecuredLoot.Items.GetValueOrDefault(itemBase.Id) + Math.Max(1, (int)dungeon.Grade);
    }

    private static List<string> ResolveExtraCombatEncounters(DungeonRun run, DungeonDefinition dungeon, RoomInstance room)
    {
        var template = dungeon.Rooms
            .Where(x => x.Type == RoomType.Combat && x.EncounterIds.Count > 0)
            .OrderByDescending(x => x.Weight)
            .FirstOrDefault();

        if (template is null)
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has no combat room template for event combat.");

        var pool = template.EncounterIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pool.Count == 0)
            throw new InvalidOperationException($"Dungeon '{dungeon.Id}' has no valid combat encounters for event combat.");

        var rand = new Random(CreateRoomSeed(run.Seed, room.RoomIndex, 31));
        var count = Math.Min(pool.Count, rand.Next(1, Math.Min(3, pool.Count) + 1));
        var result = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var index = rand.Next(pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private static int CreateRoomSeed(int runSeed, int roomIndex, int salt)
    {
        unchecked
        {
            var seed = runSeed;
            seed = (seed * 397) ^ roomIndex;
            seed = (seed * 397) ^ salt;
            return seed;
        }
    }

    private void MoveToNextRoom(DungeonRun run)
    {
        var nextRoomIndex = run.CurrentRoomIndex + 1;

        if (run.Rooms == null || nextRoomIndex >= run.Rooms.Count)
        {
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt ??= DateTimeOffset.UtcNow;
            run.CurrentRoomIndex = Math.Max(0, (run.Rooms?.Count ?? 1) - 1);
            ClearDecisionState(run);
            return;
        }

        run.State.CurrentEventChoices.Clear();
        run.State.CurrentCheckpointChoices.Clear();
        run.State.CurrentBoonChoices.Clear();

        if (_routes.GenerateRouteOptions(run).Count > 0)
        {
            return;
        }

        run.CurrentRoomIndex = nextRoomIndex;
        EnsureCurrentRoomChoices(run);
    }

    private async Task ApplyCompletionRewardsIfNeeded(DungeonRun run, CancellationToken cancellationToken)
    {
        if (run.Status != DungeonRunStatus.Completed)
        {
            return;
        }

        await _completionRewardApplier.ApplyAsync(run, cancellationToken);
    }

    private async Task RecordDungeonProgressContributionAsync(
        DungeonRun run,
        RoomInstance room,
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;

        await _guildMissionService.RecordContributionAsync(
            new GuildContributionEvent(
                run.CharacterId,
                GuildContributionSource.Dungeon,
                GuildContributionMetric.DungeonRoomsCleared,
                1,
                ContextId: run.Id.ToString(),
                OccurredAt: occurredAt,
                IdempotencyKey: $"dungeon-room-cleared:{run.Id}:{room.RoomIndex}"),
            cancellationToken);

        if (run.Status != DungeonRunStatus.Completed)
        {
            return;
        }

        await _guildMissionService.RecordContributionAsync(
            new GuildContributionEvent(
                run.CharacterId,
                GuildContributionSource.Dungeon,
                GuildContributionMetric.DungeonsCompleted,
                1,
                ContextId: run.Id.ToString(),
                OccurredAt: occurredAt,
                IdempotencyKey: $"dungeon-completed:{run.Id}"),
            cancellationToken);
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

    //    if (outcome != BattleOutcome.Victory)
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

    private void EnsureRunState(DungeonRun run)
    {
        run.State ??= new DungeonRunState();
        run.State.RunId = run.Id;
        _pressure.ApplyPressureDelta(run, 0);
        _boons.SyncActiveBoonState(run);
    }

    private DungeonRouteOption ChooseRoute(DungeonRun run, string routeOptionId)
    {
        var route = _routes.ChooseRoute(run, routeOptionId);
        if (route.PressureDelta != 0)
        {
            _pressure.ApplyPressureDelta(run, route.PressureDelta);
        }

        EnsureCurrentRoomChoices(run);
        return route;
    }

    private static void AddHiddenRouteOption(DungeonRun run)
    {
        if (run.Status != DungeonRunStatus.Active)
        {
            return;
        }

        var targetRoute = run.State.CurrentRouteOptions.FirstOrDefault();
        var targetRoomIndex = targetRoute?.RoomIndex ?? run.CurrentRoomIndex + 1;
        var targetRoom = run.Rooms.FirstOrDefault(x => x.RoomIndex == targetRoomIndex);
        if (targetRoom is null)
        {
            return;
        }

        var hiddenRouteId = $"hidden:{targetRoom.RoomIndex}";
        if (run.State.CurrentRouteOptions.Any(x => x.Id == hiddenRouteId))
        {
            return;
        }

        run.State.CurrentRouteOptions.Insert(0, new DungeonRouteOption
        {
            Id = hiddenRouteId,
            RoomIndex = targetRoom.RoomIndex,
            DisplayName = "Hidden Passage",
            RoomType = targetRoom.Type,
            RiskLevel = 1,
            PressureDelta = -8,
            IsUnknown = false,
            Tags = ["Hidden", "Shortcut"],
            PossibleRewards = ["Secret cache", "Safer path"],
            Requirements = ["Revealed by event"]
        });
    }

    private void EnsureCurrentRoomChoices(DungeonRun run)
    {
        if (run.Status != DungeonRunStatus.Active)
        {
            ClearDecisionState(run);
            return;
        }

        var currentRoom = GetCurrentRoom(run);
        var dungeon = _dungeons.GetByKey(run.DungeonDefinitionId);
        run.State.CurrentBossModifiers = _bossModifiers.GetActiveBossModifiers(run, dungeon, currentRoom).ToList();

        run.State.CurrentEventChoices.Clear();
        if (currentRoom.Type == RoomType.Event && currentRoom.Status == RoomInstanceStatus.Active)
        {
            currentRoom.EventOutcome ??= RollEventOutcome(run, currentRoom);
            _events.EnsureChoices(run, dungeon.Id, currentRoom.EventOutcome.Value);
            run.State.CurrentCheckpointChoices.Clear();
        }
        else if (currentRoom.Type == RoomType.Checkpoint)
        {
            _checkpoints.EnsureChoices(run);
        }
        else
        {
            run.State.CurrentCheckpointChoices.Clear();
        }
    }

    private async Task AddChoiceLootAsync(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room,
        string choiceId,
        CancellationToken cancellationToken)
    {
        var multiplier = choiceId == "search_deeper" ? 2 : 1;
        var pressureMultiplier = Math.Max(100, run.State.RewardMultiplierPercent) / 100m;
        var cinders = (int)Math.Ceiling(Math.Max(20, dungeon.Tier * 30) * multiplier * pressureMultiplier);
        var soulstones = choiceId == "search_deeper" ? Math.Max(1, dungeon.Tier) : 0;

        run.PendingCinders += cinders;
        run.PendingSoulstones += soulstones;
        run.State.UnsecuredLoot.Cinders += cinders;
        run.State.UnsecuredLoot.Soulstones += soulstones;

        if (choiceId == "take_supplies" || choiceId == "search_deeper")
        {
            await AddTreasureEventRewardsAsync(run, dungeon, room, cancellationToken);
        }
    }

    private void ApplyRoomCompletionPressure(DungeonRun run, RoomInstance room)
    {
        var delta = room.Type switch
        {
            RoomType.Combat => 4,
            RoomType.MiniBoss => 8,
            _ => 0
        };

        if (delta != 0)
        {
            _pressure.ApplyPressureDelta(run, delta);
        }

        if (room.Type is RoomType.Combat or RoomType.MiniBoss)
        {
            if (_boons.GenerateBoonChoices(run).Count > 0)
            {
                AddFlag(run, "pending_boon_advances_room", 1);
            }
        }
    }

    private static void AddFlag(DungeonRun run, string flag, int amount)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return;
        }

        run.State.Flags[flag] = run.State.Flags.GetValueOrDefault(flag) + amount;
    }

    private static void ReduceUnsecuredLoot(DungeonRun run, decimal percent)
    {
        var factor = Math.Clamp(1m - percent, 0m, 1m);
        run.PendingExperience = (int)Math.Floor(run.PendingExperience * factor);
        run.PendingCinders = (int)Math.Floor(run.PendingCinders * factor);
        run.PendingSoulstones = (int)Math.Floor(run.PendingSoulstones * factor);

        foreach (var reward in run.PendingRewards)
        {
            reward.Quantity = (int)Math.Floor(reward.Quantity * factor);
        }

        run.State.UnsecuredLoot = CreateLootBagFromRun(run);
    }

    private static DungeonLootBag CreateLootBagFromRun(DungeonRun run)
    {
        var bag = new DungeonLootBag
        {
            Experience = run.PendingExperience,
            Cinders = run.PendingCinders,
            Soulstones = run.PendingSoulstones
        };

        foreach (var reward in run.PendingRewards)
        {
            if (!string.IsNullOrWhiteSpace(reward.ItemId) && reward.Quantity > 0)
            {
                bag.Items[reward.ItemId] = bag.Items.GetValueOrDefault(reward.ItemId) + reward.Quantity;
            }
        }

        return bag;
    }

    private static void ClearDecisionState(DungeonRun run)
    {
        run.State.CurrentRouteOptions.Clear();
        run.State.CurrentEventChoices.Clear();
        run.State.CurrentCheckpointChoices.Clear();
        run.State.CurrentBoonChoices.Clear();
        run.State.CurrentBossModifiers.Clear();
    }

    private static string FormatPressureDelta(int delta) =>
        delta switch
        {
            > 0 => $"Pressure +{delta}.",
            < 0 => $"Pressure {delta}.",
            _ => "Pressure unchanged."
        };

    private static bool TryGetPayloadString(object? payload, string propertyName, out string value)
    {
        value = string.Empty;
        if (payload is null)
        {
            return false;
        }

        if (payload is string direct)
        {
            value = direct;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (payload is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                value = element.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }

            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        var propertyInfo = payload.GetType().GetProperty(propertyName);
        if (propertyInfo?.GetValue(payload) is string propertyValue)
        {
            value = propertyValue;
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

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
