using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessFirstHuntTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixture(string name) => Path.GetFullPath(Path.Combine(ApiRoot,
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", name));

    [Fact]
    public void Cohort_covers_authored_first_hunts_legal_reward_budgets_and_matching_draft_goals()
    {
        var (content, suite, threat, cadence) = Setup();
        var input = IdleSuite.Resolve(suite, content, threat, cadence, 1337);
        var authoredChoices = HarnessJson.Read<JsonElement>(Path.Combine(ApiRoot, "Data", "quests", "onboarding", "training-day.v4.json"))
            .GetProperty("choice").GetProperty("options").EnumerateArray()
            .Select(x => x.GetProperty("essenceDefinitionId").GetString()).Order().ToArray();
        Assert.Equal(2, input.SchemaVersion);
        Assert.Equal(36, input.Cells.Count);
        Assert.Equal(3600, input.Cells.Sum(c => c.Trials.Count));
        foreach (var stage in suite.Stages)
        {
            Assert.Equal(authoredChoices, stage.Builds.Select(b => b.EssenceIds[0]).Distinct().Order().ToArray());
            Assert.Equal(6, stage.Builds.Count);
            Assert.All(stage.Builds, build =>
            {
                Assert.Equal(new[] { "plain.mace", "plain.wand" }, stage.Builds
                    .Where(b => b.EssenceIds[0] == build.EssenceIds[0])
                    .Select(b => b.Equipment[0].DefinitionId).Order().ToArray());
                Assert.Equal(build.CharacterLevel == 1 ? 1 : build.CharacterLevel == 5 ? 2 : 3, build.Equipment.Count);
                Assert.Equal(build.CharacterLevel == 10 ? 2 : 1, build.EssenceIds.Count);
                if (build.CharacterLevel == 10) Assert.Equal("essence.goblin", build.EssenceIds[1]);
                var resolved = content.CreateBuild(build);
                Assert.All(resolved.Equipment, item =>
                {
                    Assert.Equal(1, item.ProgressionData!.State.Tier);
                    Assert.Equal(0, item.ProgressionData.State.Rank);
                    Assert.Null(item.ProgressionData.State.ActiveStyleId);
                });
                if (build.CharacterLevel >= 5)
                    Assert.Contains(resolved.Equipment.Single(e => e.ProgressionData!.State.DefinitionId == build.Equipment[1].DefinitionId).ProgressionData!.EquipmentType,
                        RandomEquipmentBoxCatalog.ArmorChest.RandomEquipment!.EquipmentTypes!);
                if (build.CharacterLevel == 10)
                    Assert.Contains(resolved.Equipment.Single(e => e.ProgressionData!.State.DefinitionId == build.Equipment[2].DefinitionId).ProgressionData!.EquipmentType,
                        RandomEquipmentBoxCatalog.JewelryChest.RandomEquipment!.EquipmentTypes!);
            });
        }
        foreach (var cell in input.Cells)
        {
            content.Validate(cell.Input);
            Assert.Equal(cell.Input.Character.Level == 1 ? 1 : 2, cell.Input.Scenario.CreatureIds.Count);
            var other = input.Cells.First(c => c.Stage == cell.Stage && c.Encounter == cell.Encounter && c.Build != cell.Build);
            Assert.Equal(cell.Trials.Select(t => t.Seed), other.Trials.Select(t => t.Seed));
        }
        var goals = BalanceGoals.Read(Fixture("idle-first-hunt-goals.json"));
        Assert.Equal(input.Cells.Select(c => c.Id).Order(), goals.RequiredCells.Order());
        Assert.Equal(BalanceGoals.FixtureContractHash(suite), goals.FixtureHash);
        Assert.Equal(180, goals.Goals.Sum(g => g.Cells.Count));
        Assert.All(goals.Goals, g => Assert.Equal(GoalEnforcement.Draft, g.Enforcement));
        Assert.All(goals.Goals.Where(g => g.Metric == GoalMetric.ClearRate), g => Assert.Null(g.Maximum));
    }

    [Fact]
    public void Legacy_contract_is_unchanged_and_group_order_is_part_of_the_new_contract()
    {
        var (content, suite, threat, cadence) = Setup();
        var old = HarnessJson.Read<IdleSuiteDefinition>(Fixture("idle-reference.json"));
        Assert.Equal("2cc0ebb584544ec70637a77ca43c43f090445d12411ddd560a54d5694b30d4ad", BalanceGoals.FixtureContractHash(old));
        var oldInput = IdleSuite.Resolve(old, content, threat, cadence, 1337);
        Assert.DoesNotContain("additionalCreature", JsonSerializer.Serialize(oldInput, HarnessJson.Options));
        var reorderedCells = suite with { Stages = suite.Stages.Reverse().Select(s => s with
            { Builds = s.Builds.Reverse().ToArray(), Encounters = s.Encounters.Reverse().ToArray() }).ToArray() };
        Assert.Equal(BalanceGoals.FixtureContractHash(suite), BalanceGoals.FixtureContractHash(reorderedCells));
        var stage = suite.Stages[1];
        var encounter = stage.Encounters[1];
        var swapped = encounter with { CreatureId = encounter.AdditionalCreatureIds![0], AdditionalCreatureIds = [encounter.CreatureId] };
        var changed = suite with { Stages = [suite.Stages[0], stage with { Encounters = [stage.Encounters[0], swapped] }, suite.Stages[2]] };
        Assert.NotEqual(BalanceGoals.FixtureContractHash(suite), BalanceGoals.FixtureContractHash(changed));
    }

    [Fact]
    public void Invalid_group_selections_and_mismatched_snapshots_are_rejected()
    {
        var (content, suite, threat, cadence) = Setup();
        var cell = IdleSuite.Resolve(suite, content, threat, cadence, 1).Cells.First(c => c.Input.Character.Level == 5);
        var scenario = cell.Input.Scenario;
        foreach (var invalid in new[]
        {
            scenario with { SchemaVersion = 1 },
            scenario with { AdditionalCreatureIds = [] },
            scenario with { AdditionalCreatureIds = [Guid.Empty] },
            scenario with { AdditionalCreatureIds = [Guid.NewGuid()] },
            scenario with { AdditionalCreatureIds = [suite.Stages[0].Encounters[0].CreatureId] },
            scenario with { AdditionalCreatureIds = Enumerable.Repeat(scenario.CreatureId, 3).ToArray() }
        })
            Assert.Throws<InvalidDataException>(() => content.CreateInput(invalid, 1, threat, cadence));
        Assert.Throws<InvalidDataException>(() => content.Validate(cell.Input with { SchemaVersion = 1 }));
        Assert.Throws<InvalidDataException>(() => content.Validate(cell.Input with { AdditionalCreatures = null }));
        Assert.Throws<JsonException>(() => content.Validate(cell.Input with { AdditionalCreatures = [cell.Input.Area] }));
        var foreign = IdleSuite.Resolve(suite, content, threat, cadence, 1).Cells[0].Input.Creature;
        Assert.Throws<InvalidDataException>(() => content.Validate(cell.Input with { AdditionalCreatures = [foreign] }));
    }

    [Fact]
    public async Task Duplicate_creatures_have_independent_state_and_detailed_runs_are_repeatable()
    {
        var (content, suite, threat, cadence) = Setup();
        var input = IdleSuite.Resolve(suite, content, threat, cadence, 1337).Cells.First(c => c.Input.Character.Level == 5).Input;
        var hash = HarnessJson.Hash(input);
        var runner = new IdleBattleRunner(content);
        var runtime = await runner.PrepareAsync(input, CancellationToken.None);
        Assert.Equal(2, runtime.HostileParticipants.Count);
        var first = runtime.HostileParticipants[0];
        var second = runtime.HostileParticipants[1];
        Assert.Equal(first.Slot.SourceEntityId, second.Slot.SourceEntityId);
        Assert.NotEqual(first.Slot.SlotId, second.Slot.SlotId);
        Assert.NotSame(first.Combatant, second.Combatant);
        Assert.NotSame(first.Combatant.BaseAttributes, second.Combatant.BaseAttributes);
        var compact = await runner.RunAsync(input);
        var detailed = await runner.RunAsync(input, true);
        var repeated = await runner.RunAsync(input);
        Assert.Equal(HarnessJson.Hash(compact.Summary), HarnessJson.Hash(detailed.Summary));
        Assert.Equal(HarnessJson.Hash(compact.Summary), HarnessJson.Hash(repeated.Summary));
        Assert.NotEmpty(detailed.EventLog!);
        Assert.Equal(hash, HarnessJson.Hash(input));
    }

    [Fact]
    public async Task Group_archives_compare_evaluate_replay_and_reject_changed_encounters()
    {
        using var workspace = new Workspace();
        var beforePath = Path.Combine(workspace.Path, "before");
        var afterPath = Path.Combine(workspace.Path, "after");
        foreach (var path in new[] { beforePath, afterPath })
        {
            var report = await SuiteBundle.CreateAsync(ApiRoot, Fixture("idle-first-hunt.json"), path, 1337, 2, CancellationToken.None);
            Assert.Equal("Complete", report.Status);
            Assert.Equal(72, report.Valid);
        }
        var baseline = Path.Combine(workspace.Path, "reference.json");
        BaselineManifest.Accept(beforePath, baseline, "Disposable test reference.");
        var comparison = SuiteComparison.Create(baseline, afterPath, Path.Combine(workspace.Path, "comparison"));
        Assert.Equal("Complete", comparison.Status);
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        var evaluation = GoalEvaluationBundle.Create(Fixture("idle-first-hunt-goals.json"), afterPath, baseline, Path.Combine(workspace.Path, "evaluation"));
        Assert.Equal(0, evaluation.ExitCode);
        Assert.Equal(180, evaluation.Checks.Count);
        Assert.All(evaluation.Checks, c => Assert.Equal(GoalOutcome.Inconclusive, c.Outcome));
        var saved = SavedSuite.Read(afterPath);
        var group = saved.Input.Cells.First(c => c.Input.Scenario.AdditionalCreatureIds is not null);
        var replay = await SuiteBundle.ReplayAsync(afterPath, group.Trials[0].BattleId, true, CancellationToken.None);
        Assert.Equal(3, replay.PreparedParticipants.GetArrayLength());
        Assert.NotEmpty(replay.EventLog!);

        var changed = group with { Input = group.Input with { Scenario = group.Input.Scenario with
            { AdditionalCreatureIds = [Guid.NewGuid()] } } };
        var different = saved with { Input = saved.Input with { Cells = saved.Input.Cells.Select(c => c.Id == group.Id ? changed : c).ToArray() } };
        var incompatible = SuiteComparison.Compare(saved, different, baseline, "Changed group test.");
        Assert.Equal("Incompatible", incompatible.Cells.Single(c => c.CellId == group.Id).Status);
        Assert.Null(incompatible.Cells.Single(c => c.CellId == group.Id).ClearRateChange);
        var legacyGoals = BalanceGoals.Read(Fixture("idle-goals.json"));
        Assert.Equal(2, GoalEvaluator.Evaluate(legacyGoals, saved, comparison).ExitCode);

        // Even if someone recomputes the input checksum, mismatched companion snapshots are invalid.
        var broken = group with { Input = group.Input with { AdditionalCreatures = null } };
        var brokenInput = saved.Input with { Cells = saved.Input.Cells.Select(c => c.Id == group.Id ? broken : c).ToArray() };
        File.WriteAllText(Path.Combine(afterPath, "suite-input.json"), JsonSerializer.Serialize(brokenInput, HarnessJson.Options));
        File.WriteAllText(Path.Combine(afterPath, "manifest.json"), JsonSerializer.Serialize(saved.Manifest with
            { InputHash = HarnessJson.Hash(brokenInput) }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => SavedSuite.Read(afterPath));
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
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-first-hunt-tests"));
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
