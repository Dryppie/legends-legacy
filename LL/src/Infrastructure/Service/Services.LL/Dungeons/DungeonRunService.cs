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
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
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
    private readonly ICharacterSnapshotRepository _characterSnapshots;
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;
    private readonly DungeonRunFactory _factory;
    private readonly IDungeonRunRewardClaimer _rewardClaimer;
    private readonly IDungeonCompletionRewardApplier _completionRewardApplier;
    private readonly IDungeonDefinitions _dungeons;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryRepository _inventory;
    private readonly IDungeonVigorService _vigor;
    private readonly IDungeonRouteService _routes;
    private readonly IDungeonCheckpointService _checkpoints;
    private readonly IDungeonEventChoiceService _events;
    private readonly IDungeonBossModifierService _bossModifiers;
    private readonly IGuildMissionService _guildMissionService;

    public DungeonRunService(
        IDungeonRunRepository dungeonRuns,
        ICharacterSnapshotRepository characterSnapshots,
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        DungeonRunFactory factory,
        IDungeonRunRewardClaimer rewardClaimer,
        IDungeonCompletionRewardApplier completionRewardApplier,
        IDungeonDefinitions dungeons,
        IItemBaseRepository itemBases,
        IInventoryRepository inventory,
        IDungeonVigorService vigor,
        IDungeonRouteService routes,
        IDungeonCheckpointService checkpoints,
        IDungeonEventChoiceService events,
        IDungeonBossModifierService bossModifiers,
        IGuildMissionService guildMissionService)
    {
        _dungeonRuns = dungeonRuns;
        _characterSnapshots = characterSnapshots;
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _factory = factory;
        _rewardClaimer = rewardClaimer;
        _completionRewardApplier = completionRewardApplier;
        _dungeons = dungeons;
        _itemBases = itemBases;
        _inventory = inventory;
        _vigor = vigor;
        _routes = routes;
        _checkpoints = checkpoints;
        _events = events;
        _bossModifiers = bossModifiers;
        _guildMissionService = guildMissionService;
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

        var seed = Random.Shared.Next(int.MinValue, int.MaxValue);

        var run = await _factory.CreateAsync(characterId, dungeonDefinitionId, seed, ct);
        _vigor.RefreshState(run);
        _routes.GenerateRouteOptions(run);

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

    public async Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid characterId, Guid runId, string actionId, object? payload, CancellationToken ct)
    {
        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(runId, ct);
        if (run == null)
            return null;

        if (run.CharacterId != characterId)
            return null;

        if (run.Status != DungeonRunStatus.Active)
            return null;

        actionId = actionId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(actionId))
            return null;

        EnsureRunState(run);
        if (run.State.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            FailRun(run, GetCurrentRoom(run), "Abandonment", "The suspended delve expired after 48 hours.");
            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.RunAbandoned,
                Message = "The suspended delve expired and its Pending Loot was lost."
            };
        }

        if (run.State.CurrentRouteOptions.Count > 0)
        {
            if (actionId.Equals(DungeonActionConstants.ChooseRoute, StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteChooseRouteActionAsync(run, payload, ct);
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
                var snapshot = run.CharacterSnapshotId.HasValue
                    ? await _characterSnapshots.GetSnapshotByIdAsync(run.CharacterSnapshotId.Value, ct)
                    : await _characterSnapshots.GetSnapshotByCharacterIdAsync(run.CharacterId, ct);
                if (snapshot == null)
                    return null;

                return await ExecuteCombatRoomAction(run, snapshot, room, actionId, payload, ct);

            case RoomType.Event:
                return await ExecuteEventRoomAction(run, room, actionId, payload, ct);

            case RoomType.Checkpoint:
                return await ExecuteCheckpointRoomAction(run, room, actionId, payload, ct);

            case RoomType.Hazard:
            case RoomType.Cache:
            case RoomType.OmenSite:
                return await ExecuteDelveNodeAction(run, room, actionId, ct);

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
                if (!run.State.WardstoneBoonChosen)
                {
                    return new ExecuteDungeonActionResult
                    {
                        Run = run,
                        Outcome = DungeonActionOutcome.None,
                        Message = "Choose one Wardstone boon before continuing."
                    };
                }
                run.State.ExtractionLocked = true;
                CompleteRoom(run, room);
                AdvanceFromWardstone(run);
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
                if (!run.State.WardstoneBoonChosen)
                {
                    return new ExecuteDungeonActionResult
                    {
                        Run = run,
                        Outcome = DungeonActionOutcome.None,
                        Message = "Choose one Wardstone boon before extracting."
                    };
                }
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

    private async Task<ExecuteDungeonActionResult?> ExecuteDelveNodeAction(
        DungeonRun run,
        RoomInstance room,
        string actionId,
        CancellationToken ct)
    {
        if (actionId.Equals(DungeonActionConstants.Leave, StringComparison.OrdinalIgnoreCase))
        {
            AbandonRun(run);
            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.RunAbandoned,
                Message = "Dungeon run abandoned. Pending Loot was lost."
            };
        }

        if (actionId is not ("continue" or "accept" or DungeonActionConstants.EventAccept))
        {
            return null;
        }

        var node = run.State.MapNodes.First(candidate => candidate.RoomIndex == room.RoomIndex);
        if (room.Type == RoomType.Hazard)
        {
            var baseToll = node.VigorCostMin == node.VigorCostMax
                ? node.VigorCostMin
                : new Random(CreateRoomSeed(run.Seed, room.RoomIndex, 83))
                    .Next(node.VigorCostMin, node.VigorCostMax + 1);
            _vigor.ApplyHazardToll(run, room, baseToll);
            ResolveLinkedAspect(run, node.BossAspectId, "Removed", $"{node.DisplayName} was overcome.");
        }
        else if (room.Type == RoomType.Cache)
        {
            var dungeon = _dungeons.GetByKey(run.DungeonDefinitionId);
            var cinders = Math.Max(30, dungeon.Tier * 45);
            var experience = Math.Max(20, dungeon.Tier * 30);
            run.PendingCinders += cinders;
            run.PendingExperience += experience;
            run.State.UnsecuredLoot.Cinders += cinders;
            run.State.UnsecuredLoot.Experience += experience;
            run.State.LastConsequence = $"Cache secured: +{cinders} Cinders and +{experience} XP added to Pending Loot.";
        }

        CompleteRoom(run, room);
        await RecordDungeonProgressContributionAsync(run, room, ct);
        if (run.State.Vigor <= 0)
        {
            FailRun(run, room, "Attrition", "Vigor was spent at the end of the encounter.");
            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.CombatDefeat,
                Message = "The party is Spent. The delve ends and Pending Loot is lost."
            };
        }

        MoveToNextRoom(run);
        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.EventResolved,
            Message = run.State.LastConsequence
        };
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
            : [];

        var playerModifiers = new List<AttributeModifierBase>();
        if (run.State.VigorState == "Exhausted")
        {
            playerModifiers.Add(new DungeonAttributeModifier(AttributeType.MaxHealth, -10, ModifierType.Additive));
        }

        var orchestrationRequest = new DungeonCombatOrchestrationRequest(
            DungeonRunId: run.Id,
            CharacterId: snapshot.CharacterId,
            CharacterSnapshot: snapshot,
            CurrentRoomIndex: run.CurrentRoomIndex,
            EnemyCreatureKeys: room.EncounterIds,
            RunAttributeModifiers: playerModifiers,
            RunAbilityModifiers: [],
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
            _vigor.ApplyCombatToll(run, room, combatSession.CombatResult);
            if (room.Type == RoomType.MiniBoss)
            {
                var node = run.State.MapNodes.FirstOrDefault(candidate => candidate.RoomIndex == room.RoomIndex);
                ResolveLinkedAspect(run, node?.BossAspectId, "Removed", $"{node?.DisplayName ?? "The miniboss"} was defeated.");
            }
            CompleteRoom(run, room);
            await RecordDungeonProgressContributionAsync(run, room, ct);
            if (run.State.Vigor <= 0)
            {
                FailRun(run, room, "Attrition", "Vigor was spent at the end of the combat.");
                outcome = DungeonActionOutcome.CombatDefeat;
            }
            else
            {
                MoveToNextRoom(run);
                await ApplyCompletionRewardsIfNeeded(run, ct);
                outcome = run.Status == DungeonRunStatus.Completed
                    ? DungeonActionOutcome.RunCompleted
                    : DungeonActionOutcome.CombatVictory;
            }
        }
        else
        {
            FailRun(run, room,
                room.Type == RoomType.Boss && run.State.BossAspects.Any(aspect => aspect.State == "Active")
                    ? "Aspect Unanswered"
                    : "Combat Readiness",
                room.Type == RoomType.Boss
                    ? "The final encounter overwhelmed the party."
                    : "The party was defeated before reaching the next Wardstone.");
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
                _vigor.ApplyHazardToll(run, room, 10);
                CompleteRoom(run, room);
                if (run.State.Vigor <= 0)
                {
                    FailRun(run, room, "Attrition", "Vigor was spent while resolving the trap.");
                    return new ExecuteDungeonActionResult
                    {
                        Run = run,
                        Outcome = DungeonActionOutcome.CombatDefeat,
                        Message = "The party is Spent. The delve ends and the Pending Loot is lost."
                    };
                }
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

    private async Task<ExecuteDungeonActionResult?> ExecuteChooseRouteActionAsync(
        DungeonRun run,
        object? payload,
        CancellationToken ct)
    {
        if (!TryGetPayloadString(payload, "routeOptionId", out var routeOptionId))
        {
            return null;
        }

        var route = ChooseRoute(run, routeOptionId);
        var room = GetCurrentRoom(run);
        if (room.Type is RoomType.Combat or RoomType.MiniBoss or RoomType.Boss)
        {
            var snapshot = run.CharacterSnapshotId.HasValue
                ? await _characterSnapshots.GetSnapshotByIdAsync(run.CharacterSnapshotId.Value, ct)
                : await _characterSnapshots.GetSnapshotByCharacterIdAsync(run.CharacterId, ct);
            return snapshot is null
                ? null
                : await ResolveCombatRoom(run, snapshot, room, ct);
        }

        if (room.Type is RoomType.Hazard or RoomType.Cache or RoomType.OmenSite)
        {
            return await ExecuteDelveNodeAction(run, room, "continue", ct);
        }

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.None,
            Message = $"{route.DisplayName} chosen. {route.Forecast}"
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
            result = await _checkpoints.ApplyChoiceAsync(run, room, choiceId, ct);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        switch (result.Outcome)
        {
            case DungeonCheckpointChoiceOutcome.Extract:
                ClearDecisionState(run);
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = "The party extracts safely. Pending Loot is secured."
                };

            case DungeonCheckpointChoiceOutcome.Recover:
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = run.State.LastConsequence
                };

            case DungeonCheckpointChoiceOutcome.Prepare:
                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = DungeonActionOutcome.CheckpointResolved,
                    Message = run.State.LastConsequence
                };

            case DungeonCheckpointChoiceOutcome.Continue:
                CompleteRoom(run, room);
                AdvanceFromWardstone(run);
                MoveToNextRoom(run);
                await RecordDungeonProgressContributionAsync(run, room, ct);
                await ApplyCompletionRewardsIfNeeded(run, ct);

                return new ExecuteDungeonActionResult
                {
                    Run = run,
                    Outcome = run.Status == DungeonRunStatus.Completed
                        ? DungeonActionOutcome.RunCompleted
                        : DungeonActionOutcome.CheckpointResolved,
                    Message = "Extraction is locked. The party continues deeper."
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

        if (run.State.Vigor <= 0)
        {
            FailRun(run, room, "Attrition", "Vigor was spent while resolving the event.");
            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.CombatDefeat,
                Message = "The party is Spent. The delve ends and the Pending Loot is lost."
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
        var cinders = Math.Max(20, dungeon.Tier * 35);
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
        var nextRoomIndexes = GetNextRoomIndexes(run);

        if (nextRoomIndexes.Count == 0)
        {
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt ??= DateTimeOffset.UtcNow;
            run.State.SecuredLoot = CreateLootBagFromRun(run);
            run.State.UnsecuredLoot = new DungeonLootBag();
            run.State.LastConsequence = "Delve completed. Pending Loot and completion rewards are secured.";
            ClearDecisionState(run);
            return;
        }

        run.State.CurrentEventChoices.Clear();
        run.State.CurrentCheckpointChoices.Clear();
        if (_routes.GenerateRouteOptions(run).Count > 0)
        {
            return;
        }

        var nextRoomIndex = nextRoomIndexes[0];
        run.CurrentRoomIndex = nextRoomIndex;
        if (!run.State.TraversedRoomIndexes.Contains(nextRoomIndex))
        {
            run.State.TraversedRoomIndexes.Add(nextRoomIndex);
        }
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
        FailRun(run, GetCurrentRoom(run), "Abandonment", "The delve was abandoned before extraction.");
    }

    private void EnsureRunState(DungeonRun run)
    {
        run.State ??= new DungeonRunState();
        run.State.RunId = run.Id;
        run.State.MapNodes ??= [];
        run.State.TraversedRoomIndexes ??= [];
        run.State.VigorHistory ??= [];
        run.State.ActiveOmens ??= [];
        run.State.BossAspects ??= [];
        NormalizeSections(run);
        if (run.State.TraversedRoomIndexes.Count == 0)
        {
            run.State.TraversedRoomIndexes.Add(run.CurrentRoomIndex);
        }
        if (run.State.ExpiresAt == default)
        {
            run.State.ExpiresAt = run.CreatedAt.AddHours(48);
        }
        _vigor.RefreshState(run);
    }

    private DungeonRouteOption ChooseRoute(DungeonRun run, string routeOptionId)
    {
        var route = _routes.ChooseRoute(run, routeOptionId);

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
        var targetRoomIndex = targetRoute?.RoomIndex ?? GetNextRoomIndexes(run).FirstOrDefault(-1);
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
            VigorCostMin = 0,
            VigorCostMax = 0,
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
            run.State.ExtractionLocked = false;
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
        var cinders = Math.Max(20, dungeon.Tier * 30) * multiplier;
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

    private static void AdvanceFromWardstone(DungeonRun run)
    {
        run.State.WardstonesReached++;
        run.State.CurrentSection = Math.Min(
            Math.Max(1, run.State.TotalSections),
            run.State.CurrentSection + 1);
        run.State.WardstoneBoonChosen = false;
        run.State.CurrentCheckpointChoices.Clear();
    }

    private static void ResolveLinkedAspect(DungeonRun run, string? aspectId, string state, string reason)
    {
        if (string.IsNullOrWhiteSpace(aspectId))
        {
            return;
        }

        var aspect = run.State.BossAspects
            .FirstOrDefault(candidate => string.Equals(candidate.Id, aspectId, StringComparison.OrdinalIgnoreCase));
        if (aspect is null)
        {
            return;
        }

        aspect.State = state;
        aspect.StateReason = reason;
        run.State.LastConsequence = $"{aspect.Name}: {state}. {reason}";
    }

    private static void FailRun(DungeonRun run, RoomInstance room, string cause, string explanation)
    {
        var lostRunLoot = CreateLootBagFromRun(run);
        var node = run.State.MapNodes.FirstOrDefault(candidate => candidate.RoomIndex == room.RoomIndex);
        run.State.FailureAnalysis = new DungeonFailureAnalysis
        {
            Location = node?.DisplayName ?? room.Type.ToString(),
            Section = node?.Section ?? run.State.CurrentSection,
            PrimaryCause = cause,
            Explanation = explanation,
            LostRunLoot = lostRunLoot,
            Suggestions = cause switch
            {
                "Aspect Unanswered" =>
                [
                    "Choose the route that removes or weakens a boss Aspect.",
                    "Extract at the Final Wardstone if Vigor is already Strained."
                ],
                "Attrition" =>
                [
                    "Take Recover at a Wardstone before entering the next Section.",
                    "Choose lower-toll routes while Vigor is Strained or Exhausted."
                ],
                "Abandonment" =>
                [
                    "Reach a Wardstone before leaving so Pending Loot can be extracted.",
                    "Use the route forecast to plan Vigor through the next breakpoint."
                ],
                _ =>
                [
                    "Improve the party's defenses or damage before retrying this tier.",
                    "Use Prepare at a Wardstone to reduce the next combat toll."
                ]
            }
        };
        run.PendingExperience = 0;
        run.PendingCinders = 0;
        run.PendingSoulstones = 0;
        run.PendingRewards.Clear();
        run.State.UnsecuredLoot = new DungeonLootBag();
        run.Status = DungeonRunStatus.Failed;
        run.DeathsDuringRun++;
        room.Status = RoomInstanceStatus.Completed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.State.LastConsequence = $"{cause}: Pending Loot was lost.";
        ClearDecisionState(run);
    }

    private static void NormalizeSections(DungeonRun run)
    {
        var state = run.State;

        var authoredSectionCount = state.MapNodes
            .Where(node => run.Rooms.Any(room =>
                room.RoomIndex == node.RoomIndex &&
                room.Type == RoomType.Checkpoint))
            .Select(node => node.Section)
            .Where(section => section > 0)
            .Distinct()
            .Count();
        var totalSections = Math.Max(1, authoredSectionCount);
        state.TotalSections = totalSections;

        var currentNodeSection = state.MapNodes
            .FirstOrDefault(node => node.RoomIndex == run.CurrentRoomIndex)
            ?.Section ?? 1;
        state.CurrentSection = Math.Clamp(
            state.CurrentSection > 0 ? state.CurrentSection : currentNodeSection,
            1,
            totalSections);

        if (state.FailureAnalysis is not null)
        {
            var failureSection = state.FailureAnalysis.Section > 0
                ? state.FailureAnalysis.Section
                : currentNodeSection;
            state.FailureAnalysis.Section = Math.Clamp(failureSection, 1, totalSections);
        }
    }

    private static void ClearDecisionState(DungeonRun run)
    {
        run.State.CurrentRouteOptions.Clear();
        run.State.CurrentEventChoices.Clear();
        run.State.CurrentCheckpointChoices.Clear();
        run.State.CurrentBossModifiers.Clear();
    }

    private static List<int> GetNextRoomIndexes(DungeonRun run)
    {
        var node = run.State.MapNodes
            .FirstOrDefault(candidate => candidate.RoomIndex == run.CurrentRoomIndex);
        if (node is not null)
        {
            return node.NextRoomIndexes
                .Where(index => run.Rooms.Any(room => room.RoomIndex == index))
                .Distinct()
                .ToList();
        }

        return [];
    }

    private static ExecuteDungeonActionResult WithdrawAndSecureLoot(DungeonRun run)
    {
        run.Status = DungeonRunStatus.Withdrawn;
        run.UsedCheckpointRetreat = true;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.State.SecuredLoot = CreateLootBagFromRun(run);
        run.State.UnsecuredLoot = new DungeonLootBag();
        ClearDecisionState(run);

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.CheckpointResolved,
            Message = "You retreated and banked the Pending Loot."
        };
    }

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
        var room = run.Rooms.FirstOrDefault(candidate => candidate.RoomIndex == run.CurrentRoomIndex);
        if (room is null) throw new InvalidOperationException("Current dungeon room state is missing.");
        return room;
    }

    private static void CompleteRoom(DungeonRun run, RoomInstance room)
    {
        room.Status = RoomInstanceStatus.Completed;
    }

}
