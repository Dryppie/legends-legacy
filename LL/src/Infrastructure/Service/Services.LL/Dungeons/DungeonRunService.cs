using Application.Interfaces.Services.LL.Dungeons;
using Domain.Helpers.Constants;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Guilds.Missions;
using Domain.Models.Inventories;
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
    private readonly IInventoryRepository _inventory;
    private readonly IDungeonVigorService _vigor;
    private readonly IDungeonRouteService _routes;
    private readonly IGuildMissionService _guildMissionService;
    private readonly IDungeonMasteryService _mastery;

    public DungeonRunService(
        IDungeonRunRepository dungeonRuns,
        ICharacterSnapshotRepository characterSnapshots,
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        DungeonRunFactory factory,
        IDungeonRunRewardClaimer rewardClaimer,
        IDungeonCompletionRewardApplier completionRewardApplier,
        IDungeonDefinitions dungeons,
        IInventoryRepository inventory,
        IDungeonVigorService vigor,
        IDungeonRouteService routes,
        IGuildMissionService guildMissionService,
        IDungeonMasteryService mastery)
    {
        _dungeonRuns = dungeonRuns;
        _characterSnapshots = characterSnapshots;
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _factory = factory;
        _rewardClaimer = rewardClaimer;
        _completionRewardApplier = completionRewardApplier;
        _dungeons = dungeons;
        _inventory = inventory;
        _vigor = vigor;
        _routes = routes;
        _guildMissionService = guildMissionService;
        _mastery = mastery;
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
        if (run == null || (run.Status != DungeonRunStatus.Completed && run.Status != DungeonRunStatus.Retreated))
            return null;

        var claimedLoot = await _rewardClaimer.ClaimAsync(run, cancellationToken);
        var result = new ClaimDungeonRewardsResult
        {
            ClaimedLoot = claimedLoot,
            WasCompleted = run.Status == DungeonRunStatus.Completed,
            DungeonDefinitionId = run.DungeonDefinitionId,
            CompletedWithoutDefeat = run.DeathsDuringRun == 0,
            CompletedWithoutRetreat = !run.UsedRetreat,
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
        var masteryByDungeon = await _mastery.GetMasteryByDungeonAsync(
            characterId,
            [dungeonDefinitionId],
            ct);
        run.State.MasteryLevelAtStart = masteryByDungeon.TryGetValue(dungeonDefinitionId, out var mastery)
            ? mastery.Level
            : 0;
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
        run.RowVersion++;
        if (run.State.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            FailRun(run, GetCurrentRoom(run), "Abandonment", "The suspended delve expired after 48 hours.");
            return new ExecuteDungeonActionResult
            {
                Run = run,
                Outcome = DungeonActionOutcome.RunFailed,
                Message = "The suspended delve expired and its Pending Loot was lost."
            };
        }

        if (actionId.Equals(DungeonActionConstants.Retreat, StringComparison.OrdinalIgnoreCase))
        {
            return RetreatAndSecureLoot(run);
        }

        if (run.State.CurrentRouteOptions.Count > 0)
        {
            if (actionId.Equals(DungeonActionConstants.ChooseRoute, StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteChooseRouteActionAsync(run, payload, ct);
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

            case RoomType.RestSite:
                return await ExecuteRestSiteRoomAction(run, room, actionId, ct);

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

            default:
                throw new InvalidOperationException(
                    $"Action '{actionId}' is not valid for room type '{room.Type}'.");
        }
    }

    private async Task<ExecuteDungeonActionResult?> ExecuteRestSiteRoomAction(
        DungeonRun run,
        RoomInstance room,
        string actionId,
        CancellationToken ct)
    {
        if (actionId.ToLowerInvariant() is not ("continue" or "rest"))
        {
            return null;
        }

        _vigor.RecoverAtRestSite(run, room);
        CompleteRoom(run, room);
        run.State.RestSitesVisited++;
        MoveToNextRoom(run);
        await RecordDungeonProgressContributionAsync(run, room, ct);
        await ApplyCompletionRewardsIfNeeded(run, ct);

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = run.Status == DungeonRunStatus.Completed
                ? DungeonActionOutcome.RunCompleted
                : DungeonActionOutcome.RestSiteResolved,
            Message = run.State.LastConsequence
        };
    }

    private async Task<ExecuteDungeonActionResult> ResolveCombatRoom(DungeonRun run, CharacterSnapshot snapshot, RoomInstance room, CancellationToken ct)
    {
        room.Status = RoomInstanceStatus.Active;
        var playerModifiers = new List<AttributeModifierBase>();
        if (run.State.VigorState == "Exhausted")
        {
            playerModifiers.Add(new DungeonAttributeModifier(AttributeType.MaxHealth, -10, ModifierType.Additive));
        }

        var dungeon = _dungeons.GetByKey(run.DungeonDefinitionId);
        var orchestrationRequest = new DungeonCombatOrchestrationRequest(
            DungeonRunId: run.Id,
            CharacterId: snapshot.CharacterId,
            CharacterSnapshot: snapshot,
            CurrentRoomIndex: run.CurrentRoomIndex,
            DungeonTier: dungeon.Tier,
            EnemyCreatureKeys: room.EncounterIds,
            RunAttributeModifiers: playerModifiers,
            RunAbilityModifiers: [],
            EnemyAttributeModifiers: [],
            EnemyStrengthMultiplier: dungeon.EnemyStrengthMultiplier);

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
                "Combat Readiness",
                room.Type == RoomType.Boss
                    ? "The final encounter overwhelmed the party."
                    : "The party was defeated before reaching the next Rest Site.");
            outcome = DungeonActionOutcome.CombatDefeat;
        }

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = outcome,
            CombatSession = combatSession
        };
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

        if (room.Type == RoomType.RestSite)
        {
            return await ExecuteRestSiteRoomAction(run, room, DungeonActionConstants.Rest, ct);
        }

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.None,
            Message = $"{route.DisplayName} chosen. {route.Forecast}"
        };
    }

    private void MoveToNextRoom(DungeonRun run)
    {
        var nextRoomIndexes = GetNextRoomIndexes(run);

        if (nextRoomIndexes.Count == 0)
        {
            run.Status = DungeonRunStatus.Completed;
            run.CompletedAt ??= DateTimeOffset.UtcNow;
            run.State.SecuredLoot = CreateLootBagFromRun(run);
            run.State.PendingLoot = new DungeonLootBag();
            run.State.LastConsequence = "Delve completed. Pending Loot and completion rewards are secured.";
            UpdatePowerPredictionOutcome(run, true, null);
            ClearDecisionState(run);
            return;
        }

        if (_routes.GenerateRouteOptions(run).Count > 0)
        {
            return;
        }

        var nextRoomIndex = nextRoomIndexes[0];
        run.CurrentRoomIndex = nextRoomIndex;
        SetCurrentSectionFromRoom(run, nextRoomIndex);
        if (!run.State.TraversedRoomIndexes.Contains(nextRoomIndex))
        {
            run.State.TraversedRoomIndexes.Add(nextRoomIndex);
        }
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

    private void EnsureRunState(DungeonRun run)
    {
        run.State ??= new DungeonRunState();
        run.State.RunId = run.Id;
        run.State.MapNodes ??= [];
        run.State.TraversedRoomIndexes ??= [];
        run.State.VigorHistory ??= [];
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
        var currentRoom = run.Rooms.FirstOrDefault(room =>
            room.RoomIndex == run.CurrentRoomIndex);
        if (run.Status == DungeonRunStatus.Active &&
            currentRoom?.Status == RoomInstanceStatus.Completed &&
            run.State.CurrentRouteOptions.Count == 0)
        {
            _routes.GenerateRouteOptions(run);
        }
    }

    private DungeonRouteOption ChooseRoute(DungeonRun run, string routeOptionId)
    {
        var route = _routes.ChooseRoute(run, routeOptionId);

        return route;
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

    private static void FailRun(DungeonRun run, RoomInstance room, string cause, string explanation)
    {
        var lostPendingLoot = CreateLootBagFromRun(run);
        var node = run.State.MapNodes.FirstOrDefault(candidate => candidate.RoomIndex == room.RoomIndex);
        run.State.FailureAnalysis = new DungeonFailureAnalysis
        {
            Location = node?.DisplayName ?? room.Type.ToString(),
            Section = node?.Section ?? run.State.CurrentSection,
            PrimaryCause = cause,
            Explanation = explanation,
            LostPendingLoot = lostPendingLoot,
            Suggestions = cause switch
            {
                "Attrition" =>
                [
                    "Take a Rest Site route before the next difficult encounter when Vigor is low.",
                    "Choose lower-toll routes while Vigor is Strained or Exhausted."
                ],
                "Abandonment" =>
                [
                    "Retreat before the run expires to secure Pending Loot.",
                    "Use the route forecast to plan Vigor through the next breakpoint."
                ],
                _ =>
                [
                    "Improve the party's defenses or damage before retrying this tier.",
                    "Retreat with Pending Loot if the next encounter is too dangerous."
                ]
            }
        };
        run.PendingExperience = 0;
        run.PendingCinders = 0;
        run.PendingSoulstones = 0;
        run.PendingRewards.Clear();
        run.State.PendingLoot = new DungeonLootBag();
        run.Status = DungeonRunStatus.Failed;
        run.DeathsDuringRun++;
        room.Status = RoomInstanceStatus.Completed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.State.LastConsequence = $"{cause}: Pending Loot was lost.";
        UpdatePowerPredictionOutcome(run, false, cause);
        ClearDecisionState(run);
    }

    private static void NormalizeSections(DungeonRun run)
    {
        var state = run.State;

        var totalSections = Math.Max(
            1,
            state.MapNodes
                .Where(node => node.Section > 0)
                .Select(node => node.Section)
                .DefaultIfEmpty(1)
                .Max());
        state.TotalSections = totalSections;

        var currentNodeSection = state.MapNodes
            .FirstOrDefault(node => node.RoomIndex == run.CurrentRoomIndex)
            ?.Section ?? 1;
        state.CurrentSection = Math.Clamp(currentNodeSection, 1, totalSections);

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
    }

    private static void SetCurrentSectionFromRoom(DungeonRun run, int roomIndex)
    {
        var section = run.State.MapNodes
            .FirstOrDefault(node => node.RoomIndex == roomIndex)
            ?.Section ?? run.State.CurrentSection;
        run.State.CurrentSection = Math.Clamp(
            section,
            1,
            Math.Max(1, run.State.TotalSections));
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

    private static ExecuteDungeonActionResult RetreatAndSecureLoot(DungeonRun run)
    {
        run.Status = DungeonRunStatus.Retreated;
        run.UsedRetreat = true;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.State.SecuredLoot = CreateLootBagFromRun(run);
        run.State.PendingLoot = new DungeonLootBag();
        run.State.LastConsequence = "Retreated safely. Pending Loot is secured.";
        UpdatePowerPredictionOutcome(run, false, "Retreat");
        ClearDecisionState(run);

        return new ExecuteDungeonActionResult
        {
            Run = run,
            Outcome = DungeonActionOutcome.RunRetreated,
            Message = "You retreated and secured the Pending Loot."
        };
    }

    private static void UpdatePowerPredictionOutcome(
        DungeonRun run,
        bool completed,
        string? failureReason)
    {
        if (run.State.PowerPrediction is not { } prediction)
            return;

        prediction.ActualCompleted = completed;
        prediction.FurthestRoomReached = run.State.TraversedRoomIndexes.DefaultIfEmpty(run.CurrentRoomIndex).Max();
        prediction.CheckpointReached = run.Rooms.Any(room =>
            room.Type == RoomType.RestSite &&
            run.State.TraversedRoomIndexes.Contains(room.RoomIndex));
        prediction.RunDurationSeconds = (int)Math.Max(
            0,
            ((run.CompletedAt ?? DateTimeOffset.UtcNow) - run.CreatedAt).TotalSeconds);
        prediction.FailureReason = failureReason;
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
