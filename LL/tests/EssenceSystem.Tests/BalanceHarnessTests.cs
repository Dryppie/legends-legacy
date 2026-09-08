using System.Text.Json;
using Application.Interfaces.Services.LL.Entities;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Idle;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string ScenarioPath => Path.GetFullPath(Path.Combine(ApiRoot,
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "idle-starter.json"));

    [Theory]
    [InlineData(1, -1, 0, "idle-reference.json", 0)]
    [InlineData(17, -1, 0, "idle-reference.json", 0)]
    [InlineData(1337, -1, 0, "idle-reference.json", 0)]
    [InlineData(-12345, -1, 0, "idle-reference.json", 0)]
    [InlineData(1337, 0, 1, "idle-reference.json", 0)]
    [InlineData(17, 1, 0, "idle-reference.json", 0)]
    [InlineData(1337, 2, 0, "idle-reference.json", 0)]
    [InlineData(17, 2, 1, "idle-reference.json", 0)]
    [InlineData(1337, 0, 0, "idle-first-hunt.json", 0)]
    [InlineData(17, 0, 3, "idle-first-hunt.json", 1)]
    [InlineData(1337, 1, 0, "idle-first-hunt.json", 0)]
    [InlineData(17, 1, 2, "idle-first-hunt.json", 1)]
    [InlineData(1337, 2, 4, "idle-first-hunt.json", 0)]
    [InlineData(17, 2, 5, "idle-first-hunt.json", 1)]
    public async Task Harness_matches_independent_production_idle_preparation_and_resolution(int seed, int stageIndex, int buildIndex,
        string suiteName, int encounterIndex)
    {
        var (content, input) = Create(seed);
        if (stageIndex >= 0)
        {
            var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(Path.GetDirectoryName(ScenarioPath)!, suiteName));
            var stage = suite.Stages[stageIndex];
            var build = stage.Builds[buildIndex];
            var encounter = stage.Encounters[encounterIndex];
            input = content.CreateInput(new IdleScenario(suite.SchemaVersion, "profile-parity", build.Id, stage.AreaId,
                encounter.CreatureId, suite.StartsAt, stage.Assumptions, build, encounter.AdditionalCreatureIds), seed,
                input.ThreatAndTanking, input.EncounterCadenceSeconds);
        }
        var actual = await new IdleBattleRunner(content).RunAsync(input);

        // Rebuild gameplay sources independently of FixtureCharacter.Materialize and runner preparation.
        Domain.Models.Entities.Characters.Character character;
        IReadOnlyList<Domain.Models.Essences.PlayerEssence> referenceEssences;
        if (input.Scenario.Build is { } profile)
        {
            var reference = content.CreateBuild(profile);
            character = reference.Character;
            referenceEssences = reference.EquippedEssences;
        }
        else
        {
            var reference = content.CreateStarter();
            character = reference.Character;
            referenceEssences = reference.EquippedEssences;
            var weapon = Assert.Single(reference.Equipment);
            character.EquipmentSlots.Add(new EquipmentSlot
            {
                EntityId = character.Id, Entity = character, EquipmentSlotType = EquipmentSlotType.MainHand,
                EquipmentInstance = weapon, EquipmentInstanceId = weapon.Id
            });
        }
        var world = HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", "world", "creatures.json"));
        var area = HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", "world", "regions.json"))
            .GetProperty("regions").EnumerateArray().SelectMany(x => x.GetProperty("areas").EnumerateArray())
            .Single(x => x.GetProperty("id").GetString() == input.Scenario.AreaId)
            .Deserialize<Domain.Models.Regions.Areas.Area>(HarnessJson.Options)!;
        var creatures = world.GetProperty("creatures").EnumerateArray()
            .Where(x => area.Creatures.Any(a => a.CreatureId == x.GetProperty("id").GetGuid()))
            .Select(x => x.Deserialize<Creature>(HarnessJson.Options)!).ToArray();
        var setup = content.CreateSetup(character, referenceEssences);
        var executor = new RecordingExecutor(content.CreateExecutor());
        var factory = new IdleCombatResolutionSessionFactory(
            new SourceEntities([character, .. creatures]), new CombatPreparationPipeline(setup), executor,
            new CombatEncounterResultFactory());
        var plan = new IdleCombatPlan(character.Id, input.Scenario.StartsAt, input.Scenario.StartsAt,
            input.Scenario.StartsAt.AddSeconds(input.EncounterCadenceSeconds),
            TimeSpan.FromSeconds(input.EncounterCadenceSeconds), 1, [character.Id], area, 1);
        var session = await factory.CreateAsync(plan, CancellationToken.None);
        var result = await session.ResolveAsync(IdleBattleRunner.CreatePlan(input), CancellationToken.None);

        Assert.Equal(HarnessJson.Hash(executor.Prepared), HarnessJson.Hash(actual.PreparedParticipants));
        Assert.Equal(HarnessJson.Hash(BattleSummary.From(result.CombatResult, 6000)), HarnessJson.Hash(actual.Summary));
        Assert.True(actual.Summary.DurationTicks > 0);
        Assert.NotEmpty(actual.Summary.Statistics.SelectMany(x => x.Abilities));
        Assert.Equal(input.Scenario.CreatureIds.Count, actual.PreparedParticipants.GetArrayLength() - 1);
    }

    [Fact]
    public async Task Repeated_parallel_and_detailed_runs_preserve_inputs_and_gameplay()
    {
        var (content, input) = Create(1337);
        var originalInputHash = HarnessJson.Hash(input);
        var runner = new IdleBattleRunner(content);
        var first = await runner.RunAsync(input);
        var detailed = await runner.RunAsync(input, detailed: true);
        Assert.Null(first.EventLog);
        Assert.NotEmpty(detailed.EventLog!);
        var parallel = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() => runner.RunAsync(input))));
        foreach (var result in parallel.Append(detailed))
        {
            Assert.Equal(HarnessJson.Hash(first.PreparedParticipants), HarnessJson.Hash(result.PreparedParticipants));
            Assert.Equal(HarnessJson.Hash(first.Summary), HarnessJson.Hash(result.Summary));
        }
        Assert.Equal(originalInputHash, HarnessJson.Hash(input));
    }

    [Fact]
    public async Task Fixture_exposes_the_opening_cooldown_parity_regression()
    {
        var (content, input) = Create(1337);
        var runner = new IdleBattleRunner(content);
        var normal = await runner.RunAsync(input);
        var runtime = await runner.PrepareAsync(input, CancellationToken.None);
        var incorrect = await content.CreateExecutor().ExecuteSimulationAsync(runtime,
            input.Rules with { StartActiveAbilitiesOnCooldown = false }, CancellationToken.None);
        Assert.NotEqual(HarnessJson.Hash(normal.Summary), HarnessJson.Hash(BattleSummary.From(incorrect, 6000)));
    }

    [Fact]
    public async Task Saved_bundle_round_trips_replays_offline_and_rejects_changed_content()
    {
        using var directory = new TemporaryDirectory();
        var run = Path.Combine(directory.Path, "run");
        var original = await RunBundle.CreateAsync(ApiRoot, ScenarioPath, run, 1337, false, CancellationToken.None);
        var replay = await RunBundle.ReplayAsync(run, true, CancellationToken.None);
        Assert.Equal(HarnessJson.Hash(original.Summary), HarnessJson.Hash(replay.Summary));
        Assert.NotEmpty(replay.EventLog!);
        Assert.False(File.Exists(Path.Combine(run, "content", "appsettings.json")));
        Assert.Equal(OfflineContent.Files.Count,
            Directory.GetFiles(Path.Combine(run, "content"), "*", SearchOption.AllDirectories).Length);
        await File.AppendAllTextAsync(Path.Combine(run, "content", "Data", "combat", "abilities.json"), " ");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            RunBundle.ReplayAsync(run, false, CancellationToken.None));
        Assert.Contains("Modified content snapshot", error.Message);
    }

    [Theory]
    [InlineData("input")]
    [InlineData("execution")]
    public async Task Replay_rejects_changed_inputs_or_execution_identity(string change)
    {
        using var directory = new TemporaryDirectory();
        var run = Path.Combine(directory.Path, "run");
        await RunBundle.CreateAsync(ApiRoot, ScenarioPath, run, 17, false, CancellationToken.None);
        if (change == "input")
        {
            var inputPath = Path.Combine(run, "input.json");
            var input = HarnessJson.Read<IdleBattleInput>(inputPath);
            await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(input with
                { Rules = input.Rules with { RandomSeed = 18 } }, HarnessJson.Options));
        }
        else
        {
            var manifestPath = Path.Combine(run, "manifest.json");
            var manifest = HarnessJson.Read<RunManifest>(manifestPath);
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest with
                { Execution = manifest.Execution with { Runtime = "different-runtime" } }, HarnessJson.Options));
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => RunBundle.ReplayAsync(run, false, CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_scenarios_and_cancellation_are_errors_not_combat_losses()
    {
        var (content, input) = Create(1);
        var unknown = input.Scenario with { CreatureId = Guid.Empty };
        Assert.Throws<InvalidDataException>(() => content.CreateInput(unknown, 1, input.ThreatAndTanking, 10));
        var invalidEssence = input with { Character = input.Character with
        {
            Essences = [new("essence.missing", 1, 0, false)]
        } };
        await Assert.ThrowsAsync<InvalidDataException>(() => new IdleBattleRunner(content).RunAsync(invalidEssence));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new IdleBattleRunner(content).RunAsync(input, cancellationToken: new CancellationToken(true)));

        using var directory = new TemporaryDirectory();
        var scenarioPath = Path.Combine(directory.Path, "invalid.json");
        HarnessJson.WriteNew(scenarioPath, unknown);
        var run = Path.Combine(directory.Path, "invalid-run");
        await Assert.ThrowsAsync<InvalidDataException>(() => RunBundle.CreateAsync(
            ApiRoot, scenarioPath, run, 1, false, CancellationToken.None));
        Assert.False(File.Exists(Path.Combine(run, "result.json")));
        Assert.Equal("Invalid", HarnessJson.Read<JsonElement>(Path.Combine(run, "failure.json"))
            .GetProperty("status").GetString());
        await Assert.ThrowsAsync<IOException>(() => RunBundle.CreateAsync(
            ApiRoot, ScenarioPath, run, 1, false, CancellationToken.None));
    }

    [Fact]
    public async Task Live_only_preparation_rejects_snapshot_requests_explicitly()
    {
        var (content, input) = Create(1);
        var character = input.Character.Materialize(content.Equipment);
        var pipeline = new CombatPreparationPipeline(content.CreateSetup(character, input.Character.MaterializeEssences()));
        var snapshot = new CharacterSnapshot { CharacterId = character.Id };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.PrepareAsync(
            CombatContentType.Idle,
            [new(new("player", character.Id, CombatSide.Friendly), new SnapshotCombatantPreparationSource(snapshot))],
            CancellationToken.None));
        Assert.Contains("requires a snapshot combatant builder", error.Message);
    }

    private static (OfflineContent Content, IdleBattleInput Input) Create(int seed)
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var combat = settings.RootElement.GetProperty("Combat");
        var threat = combat.GetProperty("ThreatAndTanking").Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)!;
        var cadence = combat.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble();
        var content = new OfflineContent(ApiRoot, threat);
        return (content, content.CreateInput(HarnessJson.Read<IdleScenario>(ScenarioPath), seed, threat, cadence));
    }

    private sealed class RecordingExecutor(ICombatEngineExecutor inner) : ICombatEngineExecutor
    {
        public JsonElement Prepared { get; private set; }
        public Task<CombatResult> ExecuteAsync(CombatEncounterRuntime runtime, CancellationToken cancellationToken)
        {
            Prepared = IdleBattleRunner.DescribeParticipants(runtime);
            return inner.ExecuteAsync(runtime, cancellationToken);
        }
    }

    private sealed class SourceEntities(IReadOnlyList<Entity> entities) : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(ids.Select(id => entities.Single(x => x.Id == id)).ToList());
        public void UpdateEntities(List<Entity> playerCharacters) => throw new InvalidOperationException("Parity tests never persist players.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-tests"));
        public string Path { get; }
        public TemporaryDirectory()
        {
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_parent, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
