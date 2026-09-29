using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Common.Randomness;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Snapshots;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration;
using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Rewards;

namespace BalanceHarness;

public sealed record DungeonAcquisitionPanel(string Dungeon, int LayoutSeed, IReadOnlyList<int> RoomSeeds);
public sealed record DungeonAcquisitionAction(int Room, RoomType Type, string Action, string? Route,
    int VigorBefore, int VigorAfter, DungeonRunStatus Status);
public sealed record DungeonAcquisitionRun(string Character, string Dungeon, int LayoutSeed,
    DungeonRunStatus Status, string? Failure, int CompletionCallbacks, int SigilsConsumed,
    IReadOnlyDictionary<string, int> EntryItems, IReadOnlyList<DungeonAcquisitionAction> Actions,
    IReadOnlyList<BattleReport> Battles, IReadOnlyList<DungeonVigorChange> VigorHistory,
    JsonElement Layout, int CombatTicks, double CombatSeconds);

/// <summary>
/// File-only production run/actions/preparation/execution. Rewards, guild contributions and mastery
/// are recorded boundaries: they cannot feed upgrades back into this fixed entry snapshot.
/// Every unimplemented boundary fails closed. No host, accounts, database or fabricated loot.
/// </summary>
public sealed class DungeonAcquisitionRunner
{
    public const int MaximumActions = 64;
    public const string RoutePolicy = "rest-first-then-lowest-visible-max-toll-min-toll-room-index-v1";
    private readonly OfflineContent content;
    private readonly JsonDungeonDefinitions definitions;
    private readonly DungeonRunFactory factory;
    private readonly Creature[] creatures;

    public DungeonAcquisitionRunner(string root, OfflineContent content)
    {
        this.content = content;
        var config = new ConfigurationBuilder().Build();
        var rewards = new JsonRewardTableDefinitionProvider(config, root, HarnessJson.Options, new RewardTableDefinitionValidator());
        definitions = new(new JsonDocumentReader<DungeonCatalogDocument>(root, "Data/dungeons/dungeons.json", HarnessJson.Options),
            new(new()), new DungeonDefinitionValidator(), rewards);
        factory = new(definitions, Boundary<ICharacterSnapshotService>(),
            new JsonDungeonDelveDefinitionProvider(config, root, HarnessJson.Options));
        creatures = HarnessJson.Read<CreatureDocument>(Path.Combine(root, "Data/world/creatures.json")).Creatures;
    }

    public static DungeonRouteOption ChooseRoute(IReadOnlyList<DungeonRouteOption> routes) => routes
        .Where(r => r.RoomType != RoomType.Treasury)
        .OrderBy(r => r.RoomType == RoomType.RestSite ? 0 : 1)
        .ThenBy(r => r.VigorCostMax).ThenBy(r => r.VigorCostMin).ThenBy(r => r.RoomIndex).First();

    public async Task<DungeonAcquisitionRun> RunAsync(FixtureCharacter character, DungeonAcquisitionPanel panel,
        Action countFight, CancellationToken token, TowerJourneyDungeon? progression = null, int masteryLevel = 0)
    {
        if (panel.RoomSeeds.Count != MaximumActions || panel.RoomSeeds.Distinct().Count() != MaximumActions)
            throw new InvalidDataException("Reserve exactly 64 distinct room seeds before execution.");
        var definition = definitions.GetByKey(panel.Dungeon);
        if (definition.Region != 1 || definition.Tier != 1)
            throw new InvalidDataException("This qualification is limited to accessible region-1 grade-I sources.");
        var run = factory.CreateForSimulation(panel.Dungeon, panel.LayoutSeed);
        run.Id = StableRandom.Guid("dungeon-acquisition-run-v1", character.Id.ToString(), panel.Dungeon, panel.LayoutSeed.ToString());
        run.CharacterId = character.Id;
        var snapshot = TowerBattleRunner.ToSnapshot(character, content);
        run.CharacterSnapshotId = snapshot.Id;
        run.CreatedAt = DateTimeOffset.UnixEpoch;
        run.State.RunId = run.Id;
        run.State.MasteryLevelAtStart = masteryLevel;
        if (masteryLevel is < 0 or > 10) throw new InvalidDataException("Invalid entry mastery.");
        run.State.ExpiresAt = DateTimeOffset.MaxValue; // bounded offline run; no wall-clock expiry or waiting claim
        var layout = JsonSerializer.SerializeToElement(new { run.Rooms, run.State.MapNodes }, HarnessJson.Options);
        var entry = definition.EntryCosts.GroupBy(c => c.ItemId).ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));
        if (entry.Count != 1 || entry.Values.Single() != 1 || !entry.ContainsKey(definition.SigilItemId))
            throw new InvalidDataException("Review changed entry economics before qualification.");
        if (progression is not null && (definition.CompletionRewardTableIds.Count != 0 || definition.TierRewardTableIds.Count != 0))
            throw new InvalidDataException("Review completion-table XP before growing-character evaluation.");
        var repository = Boundary<IDungeonRunRepository>((m, a) => m.Name switch
        {
            "GetDungeonRunByDungeonIdAsync" when (Guid)a[0]! == run.Id => Task.FromResult<DungeonRun?>(run),
            _ => throw Unexpected(m)
        });
        var snapshots = Boundary<ICharacterSnapshotRepository>((m, a) => m.Name switch
        {
            "GetSnapshotByIdAsync" when (Guid)a[0]! == snapshot.Id => Task.FromResult<CharacterSnapshot?>(snapshot),
            _ => throw Unexpected(m)
        });
        var entities = Boundary<IEntityService>((m, a) => m.Name switch
        {
            "GetEntitiesByIdsForCombatAsync" => Task.FromResult(((List<Guid>)a[0]!).Select(id => (Entity)creatures.Single(c => c.Id == id)).ToList()),
            _ => throw Unexpected(m)
        });
        var resolver = Boundary<IDungeonEncounterParticipantResolver>((m, a) => m.Name switch
        {
            "ResolveAsync" => Task.FromResult<IReadOnlyList<Guid>>(((IReadOnlyList<string>)a[0]!).Select(key =>
                creatures.Single(c => c.ImagePath.Equals(DungeonEncounterIdentity.NormalizeCreatureKey(key), StringComparison.OrdinalIgnoreCase)).Id).ToArray()),
            _ => throw Unexpected(m)
        });
        var engine = content.CreateExecutor();
        var battles = new List<BattleReport>();
        async Task<CombatResult> Execute(CombatEncounterRuntime runtime, CancellationToken ct)
        {
            countFight();
            var prepared = IdleBattleRunner.DescribeParticipants(runtime);
            // Actual production execution includes initial ability cooldowns and post-combat team state.
            var result = await engine.ExecuteAsync(runtime, captureEventLog: false, ct);
            battles.Add(new(1, $"{panel.Dungeon}/room-{run.CurrentRoomIndex}", runtime.Plan.RandomSeed!.Value,
                FastCombatEngine.TicksPerSecond, prepared, BattleSummary.From(result, 6000), null));
            return result;
        }
        var executor = Boundary<ICombatEngineExecutor>((m, a) => m.Name == "ExecuteAsync" && a.Length == 2
            ? Execute((CombatEncounterRuntime)a[0]!, (CancellationToken)a[1]!) : throw Unexpected(m));
        var productionPlanner = new DungeonCombatPlanner();
        var planner = Boundary<IDungeonCombatPlanner>((m, a) =>
        {
            if (m.Name == "CreatePlan") return m.Invoke(productionPlanner, a);
            if (m.Name != "CreateEncounterPlan") throw Unexpected(m);
            var plan = productionPlanner.CreateEncounterPlan((DungeonCombatPlan)a[0]!, (int)a[1]!, DateTimeOffset.UnixEpoch);
            if (run.CurrentRoomIndex >= MaximumActions) throw new InvalidDataException("Room reservation exhausted.");
            return plan with {
                EncounterId = StableRandom.Guid("dungeon-acquisition-encounter-v1", run.Id.ToString(), run.CurrentRoomIndex.ToString()),
                RandomSeed = panel.RoomSeeds[run.CurrentRoomIndex],
                Participants = plan.Participants.Select((p, i) => p with { SlotId = $"participant-{i}" }).ToArray()
            };
        });
        var setup = content.CreateSetup(character.Materialize(content.Equipment), character.MaterializeEssences());
        var preparation = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
        var coordinator = new CombatOrchestrationCoordinator([new DungeonCombatOrchestrator(planner,
            new DungeonCombatResolutionSessionFactory(entities, preparation, executor, new CombatEncounterResultFactory()), resolver)]);
        var factsBuilder = new Services.LL.Combat.Layers.Rewards.Dungeon.DungeonCombatRewardFactBuilder(entities, repository, definitions);
        async Task<CombatSession> Apply(CombatOutcomeRequest request, CancellationToken ct)
        {
            if (progression is not null)
            {
                var facts = await factsBuilder.BuildAsync(new(
                    (DungeonCombatOrchestrationRequest)request.OrchestrationRequest, request.OrchestrationResult,
                    (DungeonCombatOrchestrationDetails)request.OrchestrationResult.Details!), ct);
                await progression.Apply(run, facts, repository, ct);
            }
            return new() { CombatResult = request.OrchestrationResult.Encounters.Single().Resolution.CombatResult };
        }
        var outcome = Boundary<ICombatOutcomeCoordinator>((m, a) => m.Name == "ApplyAsync"
            ? Apply((CombatOutcomeRequest)a[0]!, (CancellationToken)a[1]!) : throw Unexpected(m));
        var completed = 0;
        var completion = Boundary<IDungeonCompletionRewardApplier>((m, a) =>
        {
            if (m.Name != "ApplyAsync" || !ReferenceEquals(a[0], run) || run.Status != DungeonRunStatus.Completed) throw Unexpected(m);
            completed++;
            return Task.CompletedTask;
        });
        var guild = Boundary<IGuildMissionService>((m, _) => m.Name == "RecordContributionAsync"
            ? Task.FromResult(new GuildContributionResult(true, false, 0, 0)) : throw Unexpected(m));
        var mastery = Boundary<IDungeonMasteryService>((m, _) => m.Name == "AwardRunMasteryAsync"
            ? Task.FromResult(new DungeonMasteryAwardResult(panel.Dungeon, 0, 0, 0, 0, 0, [], false)) : throw Unexpected(m));
        var vigor = new DungeonVigorService();
        var routes = new DungeonRouteService();
        vigor.RefreshState(run);
        routes.GenerateRouteOptions(run);
        var service = new DungeonRunService(repository, snapshots, coordinator, outcome, factory,
            Boundary<IDungeonRunRewardClaimer>(), completion, definitions, Boundary<IInventoryRepository>(), vigor, routes, guild, mastery);
        var actions = new List<DungeonAcquisitionAction>();
        while (run.Status == DungeonRunStatus.Active)
        {
            token.ThrowIfCancellationRequested();
            if (progression is not null) await progression.BeforeAction(token);
            if (actions.Count >= MaximumActions) throw new InvalidDataException("Action bound exceeded.");
            var route = run.State.CurrentRouteOptions.Count > 0 ? ChooseRoute(run.State.CurrentRouteOptions) : null;
            var room = run.Rooms[route?.RoomIndex ?? run.CurrentRoomIndex];
            var action = route is not null ? "choose_route" : room.Type == RoomType.RestSite ? "rest" : "fight";
            var before = run.State.Vigor;
            var result = await service.ExecuteActionAsync(character.Id, run.Id, action,
                route is null ? null : new { routeOptionId = route.Id }, token);
            if (result is null) throw new InvalidDataException("Production run rejected the fixed policy.");
            actions.Add(new(room.RoomIndex, room.Type, action, route?.Id, before, run.State.Vigor, run.Status));
            if (progression is not null) await progression.AfterAction(result.Outcome, token);
        }
        if (completed != (run.Status == DungeonRunStatus.Completed ? 1 : 0)) throw new InvalidDataException("Completion callback mismatch.");
        var ticks = battles.Sum(b => b.Summary.DurationTicks);
        if (progression is not null) await progression.Finish(run, token);
        return new(character.Name, panel.Dungeon, panel.LayoutSeed, run.Status, run.State.FailureAnalysis?.PrimaryCause,
            completed, 1, entry, actions, battles, run.State.VigorHistory, layout, ticks, ticks / (double)FastCombatEngine.TicksPerSecond);
    }

    private sealed record CreatureDocument(Creature[] Creatures);
    private static Exception Unexpected(MethodInfo method) => new InvalidOperationException($"Unmodeled offline boundary: {method.DeclaringType?.Name}.{method.Name}");
    private static T Boundary<T>(Func<MethodInfo, object?[], object?>? call = null) where T : class
    {
        var proxy = DispatchProxy.Create<T, FileBoundary>();
        ((FileBoundary)(object)proxy).Call = call ?? ((m, _) => throw Unexpected(m));
        return proxy;
    }
    public class FileBoundary : DispatchProxy
    {
        internal Func<MethodInfo, object?[], object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!, args ?? []);
    }
}
