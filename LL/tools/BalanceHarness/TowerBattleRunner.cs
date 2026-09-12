using System.Text.Json;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.PowerRatings;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record TowerPartyRecipe(int PartySlot, EquipmentReferenceBuildDefinition Build);
public sealed record TowerScenario(int SchemaVersion, string Id, int FloorNumber, DateTimeOffset StartsAt,
    string PreparationState, IReadOnlyList<string> Assumptions, IReadOnlyList<int> Seeds,
    IReadOnlyList<TowerPartyRecipe> Party);
public sealed record TowerPartyMember(int PartySlot, int PartyNumber, FixtureCharacter Character);
public sealed record TowerBattleInput(int SchemaVersion, TowerScenario Scenario, TowerFloorDefinition Floor,
    JsonElement Guardian, IReadOnlyList<TowerPartyMember> Party, ThreatAndTankingOptions ThreatAndTanking,
    int CheckpointIntervalTicks, CombatRuleset Rules);
public sealed record TowerBattleReport(BattleReport Battle, bool Succeeded, decimal GuardianHealthRemainingPercent,
    int DisplayDurationSeconds);

/// <summary>One uncleared floor with no contributions; production Tower preparation and playback.</summary>
public sealed class TowerBattleRunner(string root, OfflineContent content)
{
    public const string FloorFile = "world-tower/tower-floors.json";

    public TowerBattleInput CreateInput(TowerScenario scenario, int seed, ThreatAndTankingOptions threat,
        int checkpointIntervalTicks)
    {
        using var timing = TowerPerformanceTrace.Measure("input.materialize");
        if (scenario.SchemaVersion != 1 || string.IsNullOrWhiteSpace(scenario.Id)
            || scenario.PreparationState != "uncleared-no-contributions"
            || scenario.Seeds.Count is < 1 or > 1000 || scenario.Seeds.Distinct().Count() != scenario.Seeds.Count
            || !scenario.Seeds.Contains(seed) || checkpointIntervalTicks is < 1 or > 6000)
            throw new InvalidDataException("Tower requires schema 1, distinct declared seeds and an uncleared floor without contributions.");
        var floor = new JsonWorldTowerDefinitionProvider(Path.Combine(root, "Data", FloorFile), HarnessJson.Options)
            .GetFloor(scenario.FloorNumber) ?? throw new InvalidDataException("Unknown or unreleased Tower floor.");
        if (scenario.Party.Count != floor.RequiredSlots
            || !scenario.Party.Select(p => p.PartySlot).Order().SequenceEqual(Enumerable.Range(1, floor.RequiredSlots))
            || scenario.Party.Select(p => p.Build.Id).Distinct(StringComparer.Ordinal).Count() != floor.RequiredSlots)
            throw new InvalidDataException("Tower party must fill RequiredSlots with distinct characters and unique legal slots.");
        var party = scenario.Party.OrderBy(p => p.PartySlot).Select(p => new TowerPartyMember(
            p.PartySlot, WorldTowerPartyRules.GetPartyNumber(p.PartySlot),
            FixtureCharacter.From(content.CreateBuild(p.Build)))).ToArray();
        if (party.Select(p => p.Character.Id).Distinct().Count() != floor.RequiredSlots)
            throw new InvalidDataException("Duplicate Tower character identity.");
        var guardian = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data", "world", "creatures.json"))
            .GetProperty("creatures").EnumerateArray().SingleOrDefault(c => c.GetProperty("id").GetGuid() == floor.GuardianCreatureId);
        if (guardian.ValueKind == JsonValueKind.Undefined)
            throw new InvalidDataException("Tower guardian is missing from content.");
        return new(1, scenario, floor, guardian, party, threat, checkpointIntervalTicks,
            new(seed, MaxTicks: 6000, StartActiveAbilitiesOnCooldown: true, CaptureEventLog: false));
    }

    public async Task<CombatEncounterRuntime> PrepareAsync(TowerBattleInput input, CancellationToken token = default)
    {
        using var timing = TowerPerformanceTrace.Measure("combat.prepare-and-validate");
        token.ThrowIfCancellationRequested();
        var expected = CreateInput(input.Scenario, input.Rules.RandomSeed, input.ThreatAndTanking, input.CheckpointIntervalTicks);
        if (HarnessJson.Hash(expected) != HarnessJson.Hash(input))
            throw new InvalidDataException("Frozen Tower party, floor or rules do not match the declared recipe and content.");
        var first = input.Party[0].Character;
        var setup = content.CreateSetup(first.Materialize(content.Equipment), first.MaterializeEssences());
        var pipeline = new CombatPreparationPipeline(new FileSnapshotBuilder(content, setup), setup);
        var request = new WorldTowerCombatRuntimeRequest(
            StableRandom.Guid("balance-tower-attempt-v1", input.Scenario.Id, input.Rules.RandomSeed.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            StableRandom.Guid("balance-tower-rally-v1", input.Scenario.Id), input.Floor,
            input.Party.Select(p => new SnapshotCombatantRequest(ToSnapshot(p.Character, content),
                new(p.Character.Id.ToString(), p.Character.Id, CombatSide.Friendly, p.PartyNumber))).ToArray(),
            input.Guardian.Deserialize<Creature>(HarnessJson.Options)!, 0, 0, 0, input.Scenario.StartsAt, input.Rules.RandomSeed);
        return await new WorldTowerCombatRuntimeFactory(pipeline).CreateAsync(request, token);
    }

    public async Task<TowerBattleReport> RunAsync(TowerBattleInput input, bool detailed = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        TowerPerformanceTrace.BattleStarted();
        using var timing = TowerPerformanceTrace.Measure("battle");
        var runtime = await PrepareAsync(input, token);
        JsonElement prepared;
        using (TowerPerformanceTrace.Measure("report.describe-participants"))
            prepared = IdleBattleRunner.DescribeParticipants(runtime);
        // Separate executor/cache for every trial and replay.
        CombatEngineExecutor executor;
        using (TowerPerformanceTrace.Measure("executor.create")) executor = content.CreateExecutor();
        CombatResult result;
        using (TowerPerformanceTrace.Measure(detailed ? "engine.detailed" : "engine.playback-including-checkpoints"))
            result = detailed
            ? await executor.ExecuteSimulationAsync(runtime, input.Rules with { CaptureEventLog = true }, token)
            : (await executor.ExecuteTowerPlaybackAsync(runtime, input.CheckpointIntervalTicks, token)).Result;
        using var reportTiming = TowerPerformanceTrace.Measure("report.resolve-and-map");
        var resolution = new CombatEncounterResultFactory().Create(runtime, result);
        var guardian = resolution.HostilePostState.Single();
        var report = new TowerBattleReport(new(1, input.Scenario.Id, input.Rules.RandomSeed, FastCombatEngine.TicksPerSecond,
                prepared, BattleSummary.From(resolution.CombatResult, input.Rules.MaxTicks), detailed ? result.EventLog : null),
            resolution.Outcome == BattleOutcome.Victory,
            guardian.MaxHealth <= 0 ? 0 : Math.Round(100m * guardian.Health / guardian.MaxHealth, 2),
            Math.Max(0, (int)Math.Ceiling(result.Duration / (double)FastCombatEngine.TicksPerSecond)));
        TowerPerformanceTrace.BattleCompleted();
        return report;
    }

    public static CharacterSnapshot ToSnapshot(FixtureCharacter fixture, OfflineContent content)
    {
        var character = fixture.Materialize(content.Equipment);
        var id = StableRandom.Guid("balance-tower-snapshot-v1", fixture.Id.ToString("N"));
        return new()
        {
            Id = id, CharacterId = fixture.Id, Name = fixture.Name, Level = fixture.Level,
            CombatStyle = fixture.CombatStyle,
            BaseAttributes = character.BaseAttributes.Select(a => new EntityAttributeSnapshot
                { CharacterSnapshotId = id, AttributeType = a.AttributeType, Value = a.Value }).ToArray(),
            Equipment = character.EquipmentSlots.Select(e => EquipmentSnapshot.From(e.EquipmentSlotType, e.EquipmentInstance!)).ToArray(),
            EquippedEssences = fixture.MaterializeEssences().Select((e, i) => EquippedEssenceSnapshot.From(id, i, e)).ToArray()
        };
    }

    private sealed class FileSnapshotBuilder(OfflineContent content, ICombatSetupService setup) : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bases = requests.SelectMany(r => r.Snapshot.Equipment).Select(e => e.ItemBaseId)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(id => id,
                    id => (ItemBase)content.Equipment.GetEquipmentBase(id), StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(SnapshotCombatantBuilder.BuildFromItemBases(requests, bases, setup));
        }
    }
}
