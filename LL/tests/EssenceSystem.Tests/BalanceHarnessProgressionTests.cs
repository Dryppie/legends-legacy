using System.Text.Json;
using BalanceHarness;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessProgressionTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixture(string name) => Path.GetFullPath(Path.Combine(ApiRoot,
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", name));

    [Fact]
    public void Training_is_explicit_preserves_seed_pairing_and_keeps_old_contracts_unchanged()
    {
        var (content, suite, threat, cadence) = Setup();
        Assert.Equal("a8a7f99e9b4afed2858fbe557a045bbb85c32371f46cc2379d59c9b67e3edd87", BalanceGoals.FixtureContractHash(suite));
        Assert.DoesNotContain("essenceLevels", JsonSerializer.Serialize(suite, HarnessJson.Options));
        var stage = suite.Stages[1];
        var trained = suite with { Stages = [stage with
            { EssenceLevels = new Dictionary<string, int> { [stage.Builds[0].EssenceIds[0]] = 10 } }] };
        var before = IdleSuite.Resolve(suite with { Stages = [stage] }, content, threat, cadence, 1337);
        var after = IdleSuite.Resolve(trained, content, threat, cadence, 1337);
        Assert.NotEqual(BalanceGoals.FixtureContractHash(before.Definition), BalanceGoals.FixtureContractHash(trained));
        foreach (var cell in after.Cells)
        {
            var original = before.Cells.Single(c => c.Id == cell.Id);
            Assert.Equal(original.Trials, cell.Trials);
            Assert.Equal(original.Input.Character.Id, cell.Input.Character.Id);
            Assert.Equal(HarnessJson.Hash(original.Input.Character.Equipment), HarnessJson.Hash(cell.Input.Character.Equipment));
            var essence = Assert.Single(cell.Input.Character.Essences);
            Assert.Equal(essence.DefinitionId == stage.Builds[0].EssenceIds[0] ? 10 : 1, essence.Level);
            Assert.Equal(0, essence.AscensionTier);
            Assert.False(essence.IsEvolved);
            content.Validate(cell.Input);
        }
        Assert.All(content.CreateBuild(stage.Builds[0]).EquippedEssences, e => Assert.Equal(1, e.Level));
    }

    [Fact]
    public void Invalid_training_recipes_and_frozen_progression_mismatches_are_rejected()
    {
        var (content, suite, threat, cadence) = Setup();
        var stage = suite.Stages[1];
        var id = stage.Builds[0].EssenceIds[0];
        foreach (var levels in new Dictionary<string, int>[]
        {
            new(), new() { [id] = 0 }, new() { [id] = 11 }, new() { ["essence.unknown"] = 10 }
        })
            Assert.Throws<InvalidDataException>(() => IdleSuite.Resolve(suite with
                { Stages = [stage with { EssenceLevels = levels }] }, content, threat, cadence, 1));
        var trained = suite with { Stages = [stage with { EssenceLevels = new Dictionary<string, int> { [id] = 10 } }] };
        Assert.Throws<InvalidDataException>(() => IdleSuite.Resolve(trained with { SchemaVersion = 1 }, content, threat, cadence, 1));
        var input = IdleSuite.Resolve(trained, content, threat, cadence, 1).Cells[0].Input;
        var essence = input.Character.Essences[0];
        foreach (var invalid in new[] { essence with { Level = 1 }, essence with { AscensionTier = 1 }, essence with { IsEvolved = true } })
            Assert.Throws<InvalidDataException>(() => OfflineContent.ValidateEncounter(input with
                { Character = input.Character with { Essences = [invalid] } }));
        Assert.Throws<InvalidDataException>(() => content.CreateInput(input.Scenario with
            { EssenceLevels = new Dictionary<string, int> { [stage.Builds[2].EssenceIds[0]] = 10 } }, 1, threat, cadence));
    }

    [Fact]
    public async Task Unascended_training_changes_the_recipe_but_not_the_combat_summary()
    {
        var (content, suite, threat, cadence) = Setup();
        var stage = suite.Stages[1];
        var before = IdleSuite.Resolve(suite with { Stages = [stage] }, content, threat, cadence, 1337);
        var trained = stage with { EssenceLevels = stage.Builds.SelectMany(b => b.EssenceIds).Distinct().ToDictionary(id => id, _ => 10) };
        var after = IdleSuite.Resolve(suite with { Stages = [trained] }, content, threat, cadence, 1337);
        var runner = new IdleBattleRunner(content);
        foreach (var cell in before.Cells)
        {
            var other = after.Cells.Single(c => c.Id == cell.Id);
            Assert.NotEqual(HarnessJson.Hash(cell.Input), HarnessJson.Hash(other.Input));
            var original = await runner.RunAsync(cell.Input);
            var result = await runner.RunAsync(other.Input, detailed: true);
            Assert.Equal(HarnessJson.Hash(original.Summary), HarnessJson.Hash(result.Summary));
            Assert.NotEmpty(result.EventLog!);
        }
    }

    [Fact]
    public async Task Trained_archives_replay_compare_and_detect_rehashed_snapshot_tampering()
    {
        var (_, suite, _, _) = Setup();
        var stage = suite.Stages[1];
        var build = stage.Builds[0] with { Rank = 1 };
        var trained = suite with { SamplesPerCell = 2, Stages = [stage with
        {
            Builds = [build], Encounters = [stage.Encounters[0]],
            EssenceLevels = new Dictionary<string, int> { [build.EssenceIds[0]] = 10 }
        }] };
        using var workspace = new Workspace();
        var recipe = Path.Combine(workspace.Path, "suite.json");
        HarnessJson.WriteNew(recipe, trained);
        var first = Path.Combine(workspace.Path, "first");
        var second = Path.Combine(workspace.Path, "second");
        foreach (var path in new[] { first, second })
            Assert.Equal("Complete", (await SuiteBundle.CreateAsync(ApiRoot, recipe, path, 1337, null, CancellationToken.None)).Status);
        var saved = SavedSuite.Read(first);
        var repeated = SavedSuite.Read(second);
        var comparison = SuiteComparison.Compare(saved, repeated, "test-only", "Disposable test comparison.");
        Assert.Equal("Complete", comparison.Status);
        Assert.Equal(0, Assert.Single(comparison.Cells).GameplayChanges);
        var cell = Assert.Single(repeated.Input.Cells);
        var replay = await SuiteBundle.ReplayAsync(second, cell.Trials[0].BattleId, true, CancellationToken.None);
        Assert.NotEmpty(replay.EventLog!);
        var changed = cell with { Input = cell.Input with { Scenario = cell.Input.Scenario with
            { EssenceLevels = new Dictionary<string, int> { [build.EssenceIds[0]] = 1 } } } };
        var incompatible = SuiteComparison.Compare(saved, repeated with { Input = repeated.Input with { Cells = [changed] } }, "test-only", "Changed progression.");
        Assert.Equal("Incompatible", Assert.Single(incompatible.Cells).Status);
        Assert.Null(Assert.Single(incompatible.Cells).ClearRateChange);

        var broken = repeated.Input with { Cells = [cell with { Input = cell.Input with
            { Character = cell.Input.Character with { Essences = [cell.Input.Character.Essences[0] with { Level = 1 }] } } }] };
        File.WriteAllText(Path.Combine(second, "suite-input.json"), JsonSerializer.Serialize(broken, HarnessJson.Options));
        File.WriteAllText(Path.Combine(second, "manifest.json"), JsonSerializer.Serialize(repeated.Manifest with
            { InputHash = HarnessJson.Hash(broken) }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => SavedSuite.Read(second));
    }

    private static (OfflineContent Content, IdleSuiteDefinition Suite, ThreatAndTankingOptions Threat, double Cadence) Setup()
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var combat = settings.RootElement.GetProperty("Combat");
        var threat = combat.GetProperty("ThreatAndTanking").Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)!;
        return (new(ApiRoot, threat), HarnessJson.Read<IdleSuiteDefinition>(Fixture("idle-first-hunt.json")), threat,
            combat.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble());
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-training-tests"));
        public string Path { get; }
        public Workspace()
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
