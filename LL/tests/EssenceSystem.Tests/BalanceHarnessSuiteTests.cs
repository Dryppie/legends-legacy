using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Engine;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessSuiteTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string SuitePath => Path.GetFullPath(Path.Combine(ApiRoot,
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", "idle-reference.json"));

    [Fact]
    public void Reference_suite_has_twelve_legal_progression_cells_and_stable_paired_seeds()
    {
        var (content, definition, threat, cadence) = Setup();
        var input = IdleSuite.Resolve(definition, content, threat, cadence, 1337);
        Assert.Equal(12, input.Cells.Count);
        Assert.Equal(1200, input.Cells.Sum(x => x.Trials.Count));
        Assert.Equal([1, 5, 10], input.Cells.Select(x => x.Input.Character.Level).Distinct().ToArray());
        foreach (var cell in input.Cells)
        {
            Assert.Equal(100, cell.Trials.Select(x => x.Seed).Distinct().Count());
            Assert.Equal(cell.Input.Character.Level == 10 ? 2 : 1, cell.Input.Character.Essences.Count);
            content.Validate(cell.Input);
        }
        var reordered = definition with { Stages = definition.Stages.Reverse().Select(stage => stage with
            { Builds = stage.Builds.Reverse().ToArray(), Encounters = stage.Encounters.Reverse().ToArray() }).ToArray() };
        var reorderedInput = IdleSuite.Resolve(reordered, content, threat, cadence, 1337);
        foreach (var cell in input.Cells)
        {
            Assert.Equal(cell.Trials, reorderedInput.Cells.Single(x => x.Id == cell.Id).Trials);
            var alternative = input.Cells.First(x => x.Stage == cell.Stage && x.Encounter == cell.Encounter && x.Build != cell.Build);
            Assert.Equal(cell.Trials.Select(x => x.Seed), alternative.Trials.Select(x => x.Seed));
        }
        var otherSeed = IdleSuite.Resolve(definition, content, threat, cadence, 7331);
        Assert.NotEqual(input.Cells[0].Trials[0].Seed, otherSeed.Cells[0].Trials[0].Seed);
    }

    [Fact]
    public void Partial_builds_preserve_two_handed_identity_and_reject_illegal_equipment_or_essences()
    {
        var (content, definition, _, _) = Setup();
        var starter = definition.Stages[0].Builds[0];
        var advanced = definition.Stages[2].Builds[0];
        var reference = content.CreateBuild(advanced);
        var fixture = FixtureCharacter.From(reference);
        var materialized = fixture.Materialize(content.Equipment);
        Assert.Equal(4, fixture.Equipment.Count);
        Assert.Equal(5, materialized.EquipmentSlots.Count);
        Assert.Same(materialized.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance,
            materialized.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.OffHand).EquipmentInstance);
        Assert.Throws<ArgumentException>(() => EquipmentReferenceBuildFactory.ValidateEquipmentSlots(
            [(EquipmentSlotType.MainHand, EquipmentType.OneHanded)]));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(starter with { Tier = 2 }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(starter with
            { Equipment = [starter.Equipment[0], starter.Equipment[0]] }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(starter with
            { Equipment = [starter.Equipment[0] with { Slot = EquipmentSlotType.Head }] }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(advanced with
            { Equipment = [.. advanced.Equipment, new(EquipmentSlotType.OffHand, "plain.mace")] }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(starter with
            { EssenceIds = ["essence.goblin", "essence.goblin_warrior"] }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(advanced with
            { EssenceIds = ["essence.goblin", "essence.goblin"] }));
    }

    [Fact]
    public void Scorecard_counts_errors_separately_and_keeps_winning_and_nonwinning_durations_separate()
    {
        var (content, definition, threat, cadence) = Setup();
        var input = IdleSuite.Resolve(definition with { SamplesPerCell = 6 }, content, threat, cadence, 1);
        var cell = input.Cells[0];
        input = input with { Cells = [cell] };
        BattleObservation Row(int index, string status, BattleOutcome? outcome = null,
            double? seconds = null, double? health = null, string? ending = null) =>
            new(cell.Trials[index].BattleId, cell.Id, index, cell.Trials[index].Seed, status, outcome, ending, seconds, health);
        BattleObservation[] observations =
        [
            Row(0, "Completed", BattleOutcome.Victory, 10, 0.8),
            Row(1, "Completed", BattleOutcome.Victory, 30, 0.6),
            Row(2, "Completed", BattleOutcome.Defeat, 50, 0),
            Row(3, "Completed", BattleOutcome.Draw, 600, 0.4, "TickLimit"),
            Row(4, "Invalid")
        ];
        var report = SuiteScorecard.Create(input, observations, false, 1);
        var score = Assert.Single(report.Cells);
        Assert.Equal("Invalid", report.Status);
        Assert.Equal("Advisory", report.Policy);
        Assert.Equal((4, 1, 0, 1), (score.Valid, score.Invalid, score.Cancelled, score.NotRun));
        Assert.Equal((2, 1, 1, 1), (score.Wins, score.Losses, score.Draws, score.TickLimitDraws));
        Assert.Equal(0.5, score.ClearRate!.Rate);
        Assert.Equal(20, score.WinDurationSeconds.Median);
        Assert.Equal(325, score.NonWinDurationSeconds.Median);
        Assert.Equal(0.45, score.RemainingHealthFraction.Mean!.Value, 8);
        Assert.Null(score.WinDurationSeconds.P90);
        Assert.Throws<InvalidDataException>(() => SuiteScorecard.Create(input, [.. observations, observations[0]], false, 1));
        var noResults = SuiteScorecard.Create(input, [], true, 0);
        Assert.Equal("Cancelled", noResults.Status);
        Assert.Null(noResults.Cells[0].ClearRate);
        Assert.Null(noResults.Cells[0].DurationSeconds.Mean);
        Assert.Equal(6, noResults.NotRun);
    }

    [Fact]
    public void Wilson_intervals_and_quantiles_handle_extremes_and_small_samples()
    {
        var half = SuiteScorecard.Wilson(50, 100)!;
        Assert.InRange(half.Lower, 0.4038, 0.4039);
        Assert.InRange(half.Upper, 0.5961, 0.5962);
        Assert.InRange(SuiteScorecard.Wilson(0, 100)!.Upper, 0.0369, 0.0371);
        Assert.InRange(SuiteScorecard.Wilson(100, 100)!.Lower, 0.9629, 0.9631);
        Assert.Null(SuiteScorecard.Wilson(0, 0));
        var distribution = SuiteScorecard.Distribution(Enumerable.Range(1, 10).Select(x => (double)x));
        Assert.Equal(5.5, distribution.Median);
        Assert.Equal(9.1, distribution.P90!.Value, 8);
        Assert.Null(SuiteScorecard.Distribution([1, 2, 3]).P90);
    }

    [Fact]
    public async Task Suite_saves_one_content_snapshot_and_replays_individual_wins_and_losses()
    {
        using var directory = new TemporaryDirectory();
        var run = Path.Combine(directory.Path, "run");
        var report = await SuiteBundle.CreateAsync(ApiRoot, SuitePath, run, 1337, 3, CancellationToken.None);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(36, report.Valid);
        Assert.Equal(OfflineContent.Files.Count, Directory.GetFiles(Path.Combine(run, "content"), "*", SearchOption.AllDirectories).Length);
        Assert.Equal(36, Directory.GetFiles(Path.Combine(run, "battles")).Length);
        var rows = File.ReadLines(Path.Combine(run, "battles.jsonl"))
            .Select(line => JsonSerializer.Deserialize<BattleObservation>(line, HarnessJson.Options)!).ToArray();
        Assert.Equal(36, rows.Length);
        foreach (var outcome in new[] { BattleOutcome.Victory, BattleOutcome.Defeat })
        {
            var row = rows.First(x => x.Outcome == outcome);
            var replay = await SuiteBundle.ReplayAsync(run, row.BattleId, true, CancellationToken.None);
            Assert.Equal(outcome, replay.Summary.ContentOutcome);
            Assert.Equal(row.Seed, replay.Seed);
            Assert.NotEmpty(replay.EventLog!);
        }
        Assert.Contains("advisory only", File.ReadAllText(Path.Combine(run, "scorecard.md")));
        await Assert.ThrowsAsync<InvalidDataException>(() => SuiteBundle.ReplayAsync(run, "../escape", false, CancellationToken.None));
        var inputPath = Path.Combine(run, "suite-input.json");
        var input = HarnessJson.Read<SuiteRunInput>(inputPath);
        await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(input with { MasterSeed = 9 }, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => SuiteBundle.ReplayAsync(run, rows[0].BattleId, false, CancellationToken.None));
    }

    [Fact]
    public async Task Cancellation_preserves_completed_battles_and_marks_the_remainder_unrun()
    {
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var run = Path.Combine(directory.Path, "run");
        var report = await SuiteBundle.CreateAsync(ApiRoot, SuitePath, run, 1337, 3, cancellation.Token,
            (done, _) => { if (done == 3) cancellation.Cancel(); });
        Assert.Equal("Cancelled", report.Status);
        Assert.Equal(3, report.Valid);
        Assert.Equal(33, report.NotRun);
        Assert.Equal(0, report.Invalid);
        Assert.Equal(3, File.ReadLines(Path.Combine(run, "battles.jsonl")).Count());
        Assert.Equal("Cancelled", HarnessJson.Read<SuiteReport>(Path.Combine(run, "scorecard.json")).Status);
    }

    private static (OfflineContent Content, IdleSuiteDefinition Definition, ThreatAndTankingOptions Threat, double Cadence) Setup()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var combat = document.RootElement.GetProperty("Combat");
        var threat = combat.GetProperty("ThreatAndTanking").Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)!;
        return (new(ApiRoot, threat), HarnessJson.Read<IdleSuiteDefinition>(SuitePath), threat,
            combat.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-suite-tests"));
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
