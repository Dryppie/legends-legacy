using System.Text.Json.Nodes;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Regions.Areas;
using Services.LL.Regions;

namespace BalanceHarness;

public sealed record LocalBloodGrovePlan(string Id, int Samples, int ConfirmationSamples, int MasterSeed,
    int PlannedBattles, double OffenseMultiplier, string GoalsHash, string FixtureHash, string SettingsHash,
    IReadOnlyDictionary<string, string> ContentHashes, IReadOnlyDictionary<string, string> FixtureHashes,
    IReadOnlyList<int> ExcludedMasterSeeds, string StoppingRule, string ReplaySelection);
public sealed record LocalBloodGroveReport(string Status, int ValidBattles, GoalEvaluationReport Evaluation,
    ComparisonReport StarterComparison, IReadOnlyDictionary<string, ComparisonReport> Controls,
    int UnchangedControlCells, IReadOnlyList<PressureAreaEffect> AreaEffects, IReadOnlyList<string> Replays);

/// <summary>Fixed validation of the selected local adjustment, without searching or promoting a baseline.</summary>
public static class BloodGroveLocalValidation
{
    public const string AreaId = "region_01_area_02";
    public const string Goals = "idle-blood-grove-starter-goals.json";
    private static readonly string[] ControlFixtures = ["idle-reference.json", "idle-first-hunt.json"];
    private static readonly string[] Fixtures = [BloodGrovePressureExperiment.StarterFixture, Goals, .. ControlFixtures];

    public static RegionCombatBalanceCatalog ReadCandidateCatalog(string root)
    {
        var catalog = HarnessJson.Read<RegionCombatBalanceCatalog>(Path.Combine(root, "Data", BloodGrovePressureExperiment.BalancePath));
        _ = new RegionCreatureScalingProvider(catalog);
        var overrides = catalog.AreaOverrides ?? [];
        if (catalog.Version != 12 || overrides.Count != 2
            || !overrides.Any(x => x.AreaId == AreaId && x.OffenseMultiplier == 2.421 && x.MaximumOffenseStepIncrease is null)
            || !overrides.Any(x => x.AreaId == "region_01_area_03" && x.OffenseMultiplier is null && x.MaximumOffenseStepIncrease == 0.87)
            || catalog.Profiles.Single(x => x.Id == BloodGrovePressureExperiment.ProfileId).OffenseCurve.PostTutorialBonus != 2.3)
            throw new InvalidDataException("The fixed local candidate changed; declare a new validation protocol.");
        return catalog;
    }

    public static void CreateOriginalContentCopy(string source, string output, CancellationToken token = default)
    {
        _ = ReadCandidateCatalog(source);
        BloodGrovePressureExperiment.CreateContentCopy(source, output, 2.3, token);
        var path = Path.Combine(output, "Data", BloodGrovePressureExperiment.BalancePath);
        var node = HarnessJson.Read<JsonNode>(path);
        node.AsObject().Remove("areaOverrides");
        File.WriteAllText(path, node.ToJsonString(HarnessJson.Options));
        _ = new RegionCreatureScalingProvider(HarnessJson.Read<RegionCombatBalanceCatalog>(path));
    }

    public static LocalBloodGrovePlan CreatePlan(string root, string fixtureRoot, int samples)
    {
        if (samples is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(samples));
        _ = ReadCandidateCatalog(root);
        var goals = BalanceGoals.Read(Path.Combine(fixtureRoot, Goals));
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(fixtureRoot, BloodGrovePressureExperiment.StarterFixture));
        if (goals.Id != "idle-blood-grove-starter-goals-v2" || goals.Goals.Count != 1
            || goals.Goals[0].Minimum != 50 || goals.Goals[0].Maximum != 90
            || goals.Goals[0].Enforcement != GoalEnforcement.Enforced || goals.Goals[0].Metric != GoalMetric.ClearRate
            || goals.RequiredCells.Count != 2 || goals.FixtureHash != BalanceGoals.FixtureContractHash(suite))
            throw new InvalidDataException("The selected recipe or current policy changed.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        var content = new OfflineContent(root, threat);
        var seed = samples == 100 ? 518091 : 518092;
        int[] excludedSeeds = [1337, 7331, 940031, 620903, 318091, 318092, 418091, 418092, 418093, 418094,
            samples == 100 ? 518092 : 518091];
        var confirmation = IdleSuite.Resolve(suite with { SamplesPerCell = samples * 30 }, content, threat, cadence, seed);
        if (confirmation.Cells.Count != 2) throw new InvalidDataException("Expected two starter cells.");
        foreach (var excludedSeed in excludedSeeds)
        {
            var excluded = IdleSuite.Resolve(suite with { SamplesPerCell = 3000 }, content, threat, cadence, excludedSeed);
            foreach (var cell in confirmation.Cells)
                if (cell.Trials.Select(t => t.Seed).Intersect(excluded.Cells.Single(c => c.Id == cell.Id).Trials.Select(t => t.Seed)).Any())
                    throw new InvalidDataException("Reserved confirmation seeds overlap a previous schedule.");
        }
        var controlCells = ControlFixtures.Sum(file => IdleSuite.Resolve(
            HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(fixtureRoot, file)) with { SamplesPerCell = samples },
            content, threat, cadence, seed).Cells.Count);
        if (controlCells != 48) throw new InvalidDataException("The control suites changed.");
        return new("blood-grove-local-validation-v1", samples, samples * 30, seed, samples * 216, 2.421,
            HarnessJson.Hash(goals), goals.FixtureHash, HarnessJson.Hash(new { threat, cadence }),
            OfflineContent.Files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(root, "Data", f))),
            Fixtures.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(fixtureRoot, f))), excludedSeeds,
            "One fixed candidate. Run original/candidate confirmation and both full control suites once. No selection, tuning or sample extension. All non-Blood-Grove scaling and control gameplay must be identical. No automatic baseline promotion.",
            "Confirmation trial index 0 in both encounters, original and candidate: four outcome-independent replays.");
    }

    public static async Task<LocalBloodGroveReport> RunAsync(string root, string fixtureRoot, string outputDirectory,
        int samples, CancellationToken token, Action<string>? progress = null)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        token.ThrowIfCancellationRequested();
        var plan = CreatePlan(root, fixtureRoot, samples);
        Directory.CreateDirectory(output);
        try
        {
            HarnessJson.WriteNew(Path.Combine(output, "plan.json"), plan);
            var fixtureCopy = Path.Combine(output, "fixtures");
            Directory.CreateDirectory(fixtureCopy);
            foreach (var file in Fixtures) File.Copy(Path.Combine(fixtureRoot, file), Path.Combine(fixtureCopy, file));
            var candidateRoot = Path.Combine(output, "candidate-content");
            var originalRoot = Path.Combine(output, "original-content");
            BloodGrovePressureExperiment.CreateContentCopy(root, candidateRoot, 2.3, token);
            CreateOriginalContentCopy(candidateRoot, originalRoot, token);
            VerifySource(candidateRoot, fixtureCopy, plan);
            var candidateCatalog = ReadCandidateCatalog(candidateRoot);
            var beforeProvider = new RegionCreatureScalingProvider(candidateCatalog with { AreaOverrides = null });
            var afterProvider = new RegionCreatureScalingProvider(candidateCatalog);
            var effects = candidateCatalog.Regions.SelectMany(r => r.AreaIds).Select(id =>
            {
                var before = beforeProvider.GetScaling(new Area { Id = id });
                var after = afterProvider.GetScaling(new Area { Id = id });
                if (id == AreaId ? before with { OffenseMultiplier = 2.421 } != after : before != after)
                    throw new InvalidDataException($"Unexpected scaling change in {id}.");
                return new PressureAreaEffect(id, before.OffenseMultiplier, after.OffenseMultiplier,
                    Math.Sqrt(after.OffenseMultiplier / before.OffenseMultiplier));
            }).ToArray();
            var executionHash = HarnessJson.Hash(ExecutionIdentity.Current());
            var valid = 0;
            async Task<SavedSuite> Run(string contentRoot, string fixture, string relative, int count)
            {
                token.ThrowIfCancellationRequested();
                progress?.Invoke($"{relative}: {count} trials per cell");
                var path = Path.Combine(output, relative);
                var result = await SuiteBundle.CreateAsync(contentRoot, Path.Combine(fixtureCopy, fixture), path, plan.MasterSeed, count, token);
                if (result.Status != "Complete")
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidDataException($"Incomplete validation run: {relative}");
                }
                var saved = SavedSuite.Read(path, token);
                if (HarnessJson.Hash(saved.Manifest.Execution) != executionHash
                    || OfflineContent.Files.Any(f => saved.Manifest.ContentHashes[f] != HarnessJson.FileHash(Path.Combine(contentRoot, "Data", f))))
                    throw new InvalidDataException("Content or executable identity changed during validation.");
                valid += result.Valid;
                return saved;
            }
            ComparisonReport Compare(SavedSuite before, SavedSuite after, string relative)
            {
                var baseline = Path.Combine(output, relative, "control-baseline.json");
                BaselineManifest.Accept(before.Directory, baseline, "Original regional content control; not a viable gameplay baseline.", token);
                var comparison = SuiteComparison.Create(baseline, after.Directory, Path.Combine(output, relative, "comparison"), token);
                if (comparison.Status != "Complete") throw new InvalidDataException("Validation runs are incompatible.");
                return comparison;
            }
            var original = await Run(originalRoot, BloodGrovePressureExperiment.StarterFixture, "confirmation/original", plan.ConfirmationSamples);
            var candidate = await Run(candidateRoot, BloodGrovePressureExperiment.StarterFixture, "confirmation/candidate", plan.ConfirmationSamples);
            _ = GoalEvaluationBundle.Create(Path.Combine(fixtureCopy, Goals), original.Directory, null, Path.Combine(output, "evaluation/original"), token);
            var evaluation = GoalEvaluationBundle.Create(Path.Combine(fixtureCopy, Goals), candidate.Directory, null, Path.Combine(output, "evaluation/candidate"), token);
            var starterComparison = Compare(original, candidate, "starter-comparison");
            var controls = new Dictionary<string, ComparisonReport>();
            var unchanged = 0;
            foreach (var fixture in ControlFixtures)
            {
                var id = Path.GetFileNameWithoutExtension(fixture);
                var before = await Run(originalRoot, fixture, $"controls/{id}/original", samples);
                var after = await Run(candidateRoot, fixture, $"controls/{id}/candidate", samples);
                var comparison = Compare(before, after, $"controls/{id}");
                foreach (var cell in before.Input.Cells.Where(c => c.Input.Scenario.AreaId != AreaId))
                {
                    if (comparison.Cells.Single(c => c.CellId == cell.Id).GameplayChanges != 0)
                        throw new InvalidDataException($"Unaffected control changed: {cell.Id}.");
                    unchanged++;
                }
                controls.Add(id, comparison);
            }
            var replays = new List<string>();
            Directory.CreateDirectory(Path.Combine(output, "replays"));
            foreach (var saved in new[] { original, candidate })
            foreach (var cell in saved.Input.Cells)
            {
                var replay = await SuiteBundle.ReplayAsync(saved.Directory, cell.Trials[0].BattleId, true, token);
                var relative = $"replays/{Path.GetFileName(saved.Directory)}-{cell.Trials[0].BattleId}.json";
                HarnessJson.WriteNew(Path.Combine(output, relative), replay);
                replays.Add(relative);
            }
            VerifySource(root, fixtureRoot, plan);
            VerifySource(candidateRoot, fixtureCopy, plan);
            if (valid != plan.PlannedBattles || unchanged != 32) throw new InvalidDataException("Validation budget or control coverage is incomplete.");
            var report = new LocalBloodGroveReport("Complete", valid, evaluation, starterComparison, controls, unchanged, effects, replays);
            HarnessJson.WriteNew(Path.Combine(output, "results.json"), report);
            await File.WriteAllTextAsync(Path.Combine(output, "summary.md"),
                $"# Blood Grove local validation\n\nComplete: {valid} valid battles. Current policy: **{evaluation.GateStatus}**.\n\n" +
                "Only Blood Grove offense and its coupled regeneration changed; the other 13 authored areas retain scaling. " +
                $"All {unchanged} non-Blood-Grove control cells preserve gameplay exactly. All four fixed replays matched.\n\n" +
                "Inspect evaluation/candidate/evaluation.md and starter-comparison/comparison/comparison.md for per-encounter intervals. " +
                "Crystal Creek retains its original strength with an explicit 87% offense entry ceiling. No baseline is automatically promoted.\n", token);
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            { Status = exception is OperationCanceledException ? "Cancelled" : "Invalid", ErrorType = exception.GetType().Name, exception.Message });
            throw;
        }
    }

    private static void VerifySource(string root, string fixtureRoot, LocalBloodGrovePlan plan)
    {
        if (plan.ContentHashes.Any(p => p.Value != HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)))
            || plan.FixtureHashes.Any(p => p.Value != HarnessJson.FileHash(Path.Combine(fixtureRoot, p.Key))))
            throw new InvalidDataException("Source content or fixtures changed during validation.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        if (HarnessJson.Hash(new { threat, cadence }) != plan.SettingsHash)
            throw new InvalidDataException("Combat settings changed during validation.");
    }
}
