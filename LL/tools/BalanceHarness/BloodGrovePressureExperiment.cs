using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Regions.Areas;
using Services.LL.Regions;

namespace BalanceHarness;

public sealed record PressureCandidate(string Id, double Bonus);
public sealed record PressurePlan(int SchemaVersion, string ProfileId, string Field,
    IReadOnlyList<PressureCandidate> Candidates, IReadOnlyDictionary<string, string> RejectedCandidates,
    int Samples, int ConfirmationSamples, int DiscoverySeed, int ConfirmationSeed, int PlannedBattles,
    string FixtureHash, string GoalsHash, string SettingsHash,
    IReadOnlyDictionary<string, string> ContentHashes, IReadOnlyDictionary<string, string> FixtureFileHashes,
    string SelectionRule, string StoppingRule, string ReplaySelection);
public sealed record PressureFinding(PressureCandidate Candidate, IReadOnlyList<CellScorecard> Cells,
    double MaximumDeviation, double MeanDeviation, string ArtifactHash);
public sealed record PressureAreaEffect(string AreaId, double BeforeOffense, double AfterOffense,
    double RegenerationFactor);
public sealed record PressureReport(string Status, int ValidBattles, PressureFinding Selected,
    IReadOnlyList<PressureFinding> Discovery, GoalEvaluationReport Confirmation,
    ComparisonReport StarterComparison, IReadOnlyDictionary<string, ComparisonReport> Controls,
    int UnchangedOpeningControlCells, IReadOnlyList<PressureAreaEffect> AreaEffects,
    IReadOnlyList<string> Replays, string Disposition);

/// <summary>A local, single-parameter search with selection frozen before confirmation.</summary>
public static class BloodGrovePressureExperiment
{
    public const string BalancePath = "progression/region-combat-balance.json";
    public const string ProfileId = "gated-region-one-v1";
    public const string StarterFixture = "idle-blood-grove-starter.json";
    public const string GoalFixture = "idle-blood-grove-starter-goals.json";
    private static readonly string[] ControlFixtures = ["idle-reference.json", "idle-first-hunt.json"];
    private static readonly string[] Fixtures = [StarterFixture, GoalFixture, .. ControlFixtures];
    private const double OriginalBonus = 2.3;

    public static PressurePlan CreatePlan(string apiRoot, string fixtureRoot, int samples)
    {
        if (samples is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(samples), "Use 1–100 discovery/control samples; confirmation uses ten times that count.");
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(fixtureRoot, StarterFixture));
        var goals = BalanceGoals.Read(Path.Combine(fixtureRoot, GoalFixture));
        var goal = goals.Goals.Single();
        var fixtureHash = BalanceGoals.FixtureContractHash(suite);
        if (suite.Id != "idle-blood-grove-starter-v1" || fixtureHash != goals.FixtureHash
            || goal.Metric != GoalMetric.ClearRate || goal.Minimum != 65 || goal.Maximum != 75
            || goal.Enforcement != GoalEnforcement.Enforced || goals.RequiredCells.Count != 2)
            throw new InvalidDataException("Review the changed starter recipe/policy before this fixed experiment.");
        var catalog = HarnessJson.Read<RegionCombatBalanceCatalog>(Path.Combine(apiRoot, "Data", BalancePath));
        if (catalog.Profiles.Single(p => p.Id == ProfileId).OffenseCurve.PostTutorialBonus != OriginalBonus)
            throw new InvalidDataException("The source offense bonus changed; review the search bounds and control.");
        var candidates = new List<PressureCandidate>();
        var rejected = new Dictionary<string, string>();
        // Validate the entire declared grid before any battle. Never relax production validation.
        for (var tenth = 23; tenth >= 0; tenth--)
        {
            var candidate = new PressureCandidate($"bonus-{tenth * 10:D3}", tenth / 10d);
            try { _ = new RegionCreatureScalingProvider(WithBonus(catalog, candidate.Bonus)); candidates.Add(candidate); }
            catch (InvalidOperationException exception) { rejected.Add(candidate.Id, exception.Message); }
        }
        if (candidates.Count < 2 || candidates[0].Bonus != OriginalBonus)
            throw new InvalidDataException("The control and at least one legal substitution are required.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(apiRoot);
        var content = new OfflineContent(apiRoot, threat);
        var discovery = IdleSuite.Resolve(suite with { SamplesPerCell = samples }, content, threat, cadence, 1337);
        // Small workflow checks use a separate set so they never consume the full run's reserved seeds.
        var confirmationSeed = samples == 100 ? 318091 : 318092;
        var confirmation = IdleSuite.Resolve(suite with { SamplesPerCell = samples * 10 }, content, threat, cadence, confirmationSeed);
        if (discovery.Cells.Count != 2) throw new InvalidDataException("Expected two starter cells.");
        RequireDisjoint(discovery, confirmation);
        var controls = ControlFixtures.Sum(file => IdleSuite.Resolve(
            HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(fixtureRoot, file)) with { SamplesPerCell = samples },
            content, threat, cadence, confirmationSeed).Cells.Count);
        return new(1, ProfileId, "offenseCurve.postTutorialBonus", candidates, rejected, samples, samples * 10,
            1337, confirmationSeed, samples * (2 * candidates.Count + 40 + 2 * controls), fixtureHash, HarnessJson.Hash(goals),
            HarnessJson.Hash(new { threat, cadence }),
            OfflineContent.Files.ToDictionary(p => p, p => HarnessJson.FileHash(Path.Combine(apiRoot, "Data", p))),
            Fixtures.ToDictionary(p => p, p => HarnessJson.FileHash(Path.Combine(fixtureRoot, p))),
            "After all discovery runs, minimize the worst per-encounter absolute distance from 70%; break ties by mean distance, then the largest bonus (least change). Freeze the selection before reading confirmation outcomes, even if no candidate is near the band.",
            "Run every legal preflight candidate once on discovery; then the original and selected candidate on reserved confirmation seeds and both complete control suites. No refinement, replacement selection or sample extension after outcomes. A complete investigation may fail the gameplay target. No production promotion.",
            "Confirmation trial index 0 for both encounters, for original and selected candidate: four detailed replays, independent of outcomes.");
    }

    public static RegionCombatBalanceCatalog WithBonus(RegionCombatBalanceCatalog source, double bonus) => source with
    {
        Profiles = source.Profiles.Select(p => p.Id == ProfileId
            ? p with { OffenseCurve = p.OffenseCurve with { PostTutorialBonus = bonus } } : p).ToArray()
    };

    public static void CreateContentCopy(string source, string output, double bonus, CancellationToken token)
    {
        if (Path.Exists(output)) throw new IOException($"Content output already exists: {output}");
        if (!double.IsFinite(bonus) || bonus < 0 || bonus > OriginalBonus) throw new ArgumentOutOfRangeException(nameof(bonus));
        token.ThrowIfCancellationRequested();
        var catalog = HarnessJson.Read<RegionCombatBalanceCatalog>(Path.Combine(source, "Data", BalancePath));
        _ = new RegionCreatureScalingProvider(WithBonus(catalog, bonus));
        RunBundle.CopyContent(source, output, token);
        var (threat, cadence) = RunBundle.ReadCombatSettings(source);
        // Only selected nonsecret settings are written; never copy API appsettings.
        HarnessJson.WriteNew(Path.Combine(output, "appsettings.json"), new Dictionary<string, object>
        {
            ["Combat"] = new Dictionary<string, object>
            {
                ["ThreatAndTanking"] = threat,
                ["IdleProgression"] = new Dictionary<string, double> { ["EncounterCadenceSeconds"] = cadence }
            }
        });
        if (bonus == OriginalBonus) return;
        var path = Path.Combine(output, "Data", BalancePath);
        var node = JsonNode.Parse(File.ReadAllText(path))!;
        var profile = node["profiles"]!.AsArray().Single(p => p!["id"]!.GetValue<string>() == ProfileId)!;
        profile["offenseCurve"]!["postTutorialBonus"] = bonus;
        File.WriteAllText(path, node.ToJsonString(HarnessJson.Options));
    }

    public static PressureFinding SelectCandidate(IEnumerable<PressureFinding> findings) => findings
        .OrderBy(f => f.MaximumDeviation).ThenBy(f => f.MeanDeviation).ThenByDescending(f => f.Candidate.Bonus).First();

    public static async Task<PressureReport> RunAsync(string apiRoot, string fixtureRoot, string outputDirectory,
        int samples, CancellationToken token, Action<string>? progress = null)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        token.ThrowIfCancellationRequested();
        var plan = CreatePlan(apiRoot, fixtureRoot, samples);
        Directory.CreateDirectory(output);
        try
        {
            HarnessJson.WriteNew(Path.Combine(output, "plan.json"), plan);
            var capturedFixtures = Path.Combine(output, "fixtures");
            Directory.CreateDirectory(capturedFixtures);
            foreach (var file in Fixtures)
            {
                File.Copy(Path.Combine(fixtureRoot, file), Path.Combine(capturedFixtures, file));
                if (HarnessJson.FileHash(Path.Combine(capturedFixtures, file)) != plan.FixtureFileHashes[file])
                    throw new InvalidDataException("Fixture changed during capture.");
            }
            var source = Path.Combine(output, "source-content");
            CreateContentCopy(apiRoot, source, OriginalBonus, token);
            VerifySource(source, plan);
            var execution = HarnessJson.Hash(ExecutionIdentity.Current());
            var valid = 0;
            async Task<SavedSuite> Run(string root, string fixture, string relative, int seed, int count)
            {
                token.ThrowIfCancellationRequested();
                progress?.Invoke($"{relative}: {count} trials per cell");
                var directory = Path.Combine(output, relative);
                var result = await SuiteBundle.CreateAsync(root, Path.Combine(capturedFixtures, fixture), directory,
                    seed, count, token);
                if (result.Status != "Complete")
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidDataException($"Incomplete experiment run: {relative}");
                }
                var saved = SavedSuite.Read(directory, token);
                if (HarnessJson.Hash(saved.Manifest.Execution) != execution)
                    throw new InvalidDataException("Execution changed during the experiment.");
                foreach (var file in OfflineContent.Files)
                    if (saved.Manifest.ContentHashes[file] != HarnessJson.FileHash(Path.Combine(root, "Data", file)))
                        throw new InvalidDataException("Content changed during a run.");
                valid += result.Valid;
                return saved;
            }
            var findings = new List<PressureFinding>();
            var roots = new Dictionary<string, string>();
            SuiteRunInput? discoveryInput = null;
            foreach (var candidate in plan.Candidates)
            {
                var root = candidate.Bonus == OriginalBonus ? source : Path.Combine(output, "candidates", candidate.Id);
                if (root != source) CreateContentCopy(source, root, candidate.Bonus, token);
                roots.Add(candidate.Id, root);
                var saved = await Run(root, StarterFixture, "discovery/" + candidate.Id, plan.DiscoverySeed, samples);
                discoveryInput ??= saved.Input;
                var deviations = saved.Scorecard.Cells.Select(c => Math.Abs(c.ClearRate!.Rate * 100 - 70)).ToArray();
                findings.Add(new(candidate, saved.Scorecard.Cells, deviations.Max(), deviations.Average(), saved.ArtifactHash));
            }
            var selected = SelectCandidate(findings);
            HarnessJson.WriteNew(Path.Combine(output, "selection.json"), selected);
            progress?.Invoke($"Selection frozen: {selected.Candidate.Id}; worst discovery distance {selected.MaximumDeviation:0.0} percentage points from 70%.");
            var before = await Run(source, StarterFixture, "confirmation/original", plan.ConfirmationSeed, plan.ConfirmationSamples);
            var after = await Run(roots[selected.Candidate.Id], StarterFixture, "confirmation/candidate", plan.ConfirmationSeed, plan.ConfirmationSamples);
            RequireDisjoint(discoveryInput!, after.Input);
            var goalPath = Path.Combine(capturedFixtures, GoalFixture);
            _ = GoalEvaluationBundle.Create(goalPath, before.Directory, null, Path.Combine(output, "evaluation/original"), token);
            var assessment = GoalEvaluationBundle.Create(goalPath, after.Directory, null, Path.Combine(output, "evaluation/candidate"), token);
            ComparisonReport Compare(SavedSuite original, SavedSuite candidate, string relative)
            {
                var baseline = Path.Combine(output, relative, "control-baseline.json");
                BaselineManifest.Accept(original.Directory, baseline, "Experimental original-content control only; not a viable or approved difficulty baseline.", token);
                var comparison = SuiteComparison.Create(baseline, candidate.Directory, Path.Combine(output, relative, "comparison"), token);
                if (comparison.Status != "Complete") throw new InvalidDataException("Controlled content substitution is incompatible.");
                return comparison;
            }
            var starterComparison = Compare(before, after, "starter-comparison");
            var comparisons = new Dictionary<string, ComparisonReport>();
            var unchangedOpening = 0;
            foreach (var fixture in ControlFixtures)
            {
                var id = Path.GetFileNameWithoutExtension(fixture);
                var original = await Run(source, fixture, $"controls/{id}/original", plan.ConfirmationSeed, samples);
                var candidate = await Run(roots[selected.Candidate.Id], fixture, $"controls/{id}/candidate", plan.ConfirmationSeed, samples);
                var comparison = Compare(original, candidate, $"controls/{id}");
                foreach (var cell in original.Input.Cells.Where(c => c.Input.Scenario.AreaId == "region_01_area_01"))
                {
                    if (comparison.Cells.Single(c => c.CellId == cell.Id).GameplayChanges != 0)
                        throw new InvalidDataException("The declared unaffected opening-area control changed.");
                    unchangedOpening++;
                }
                comparisons.Add(id, comparison);
            }
            var catalog = HarnessJson.Read<RegionCombatBalanceCatalog>(Path.Combine(source, "Data", BalancePath));
            var originalProvider = new RegionCreatureScalingProvider(catalog);
            var candidateProvider = new RegionCreatureScalingProvider(WithBonus(catalog, selected.Candidate.Bonus));
            var effects = catalog.Regions.SelectMany(r => r.AreaIds).Select(id =>
            {
                var a = originalProvider.GetScaling(new Area { Id = id });
                var b = candidateProvider.GetScaling(new Area { Id = id });
                if (a with { OffenseMultiplier = b.OffenseMultiplier } != b)
                    throw new InvalidDataException("The candidate changed a scaling field outside the declared offense axis.");
                return new PressureAreaEffect(id, a.OffenseMultiplier, b.OffenseMultiplier,
                    Math.Sqrt(b.OffenseMultiplier / a.OffenseMultiplier));
            }).ToArray();
            var replays = new List<string>();
            foreach (var saved in new[] { before, after })
            foreach (var cell in saved.Input.Cells)
            {
                var replay = await SuiteBundle.ReplayAsync(saved.Directory, cell.Trials[0].BattleId, true, token);
                var relative = "replays/" + Path.GetFileName(saved.Directory) + "-" + cell.Trials[0].BattleId + ".json";
                Directory.CreateDirectory(Path.Combine(output, "replays"));
                HarnessJson.WriteNew(Path.Combine(output, relative), replay);
                replays.Add(relative);
            }
            VerifySource(apiRoot, plan);
            if (Fixtures.Any(f => HarnessJson.FileHash(Path.Combine(fixtureRoot, f)) != plan.FixtureFileHashes[f]))
                throw new InvalidDataException("Source fixtures changed during the experiment.");
            if (valid != plan.PlannedBattles) throw new InvalidDataException("The full declared battle budget did not complete.");
            var report = new PressureReport("Complete", valid, selected, findings, assessment, starterComparison,
                comparisons, unchangedOpening, effects, replays,
                assessment.GateStatus == "Pass" ? "Working band met on reserved seeds; collateral effects require review. No production changes or viable-baseline promotion."
                    : "Working band not confirmed. Retain this completed investigation; do not promote, reselect or extend samples.");
            HarnessJson.WriteNew(Path.Combine(output, "results.json"), report);
            await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), Markdown(plan, report), token);
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            { Status = exception is OperationCanceledException ? "Cancelled" : "Invalid", ErrorType = exception.GetType().Name, exception.Message });
            throw;
        }
    }

    private static void VerifySource(string root, PressurePlan plan)
    {
        foreach (var pair in plan.ContentHashes)
            if (HarnessJson.FileHash(Path.Combine(root, "Data", pair.Key)) != pair.Value)
                throw new InvalidDataException("Source combat content changed.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        if (HarnessJson.Hash(new { threat, cadence }) != plan.SettingsHash)
            throw new InvalidDataException("Source combat settings changed.");
    }

    private static void RequireDisjoint(SuiteRunInput discovery, SuiteRunInput confirmation)
    {
        foreach (var cell in confirmation.Cells)
            if (cell.Trials.Select(t => t.Seed).Intersect(discovery.Cells.Single(c => c.Id == cell.Id).Trials.Select(t => t.Seed)).Any())
                throw new InvalidDataException("Discovery and confirmation schedules overlap.");
    }

    private static string Markdown(PressurePlan plan, PressureReport report)
    {
        static string N(double? value) => value?.ToString("0.0", CultureInfo.InvariantCulture) ?? "—";
        var text = new StringBuilder("# Blood Grove regional offense experiment\n\n");
        text.AppendLine($"{report.Status}: {report.ValidBattles}/{plan.PlannedBattles} valid battles. {report.Disposition}\n");
        text.AppendLine($"Selected bonus: {N(report.Selected.Candidate.Bonus)} (original 2.3). Selection used discovery only; confirmation gate: **{report.Confirmation.GateStatus}**.\n");
        text.AppendLine("| Discovery bonus | Encounter | Wins / trials | Clear % [95% Wilson] |\n| --- | --- | --- | --- |");
        foreach (var finding in report.Discovery)
        foreach (var cell in finding.Cells)
            text.AppendLine($"| {N(finding.Candidate.Bonus)} | {cell.Encounter} | {cell.Wins}/{cell.Valid} | {N(cell.ClearRate!.Rate * 100)} [{N(cell.ClearRate.Lower * 100)}, {N(cell.ClearRate.Upper * 100)}] |");
        text.AppendLine("\n| Confirmation encounter | Original wins | Candidate wins | Candidate clear % [95% Wilson] | Paired change pp |\n| --- | --- | --- | --- | --- |");
        foreach (var cell in report.StarterComparison.Cells)
            text.AppendLine($"| {cell.Candidate!.Encounter} | {cell.Baseline!.Wins}/{cell.Baseline.Valid} | {cell.Candidate.Wins}/{cell.Candidate.Valid} | {N(cell.Candidate.ClearRate!.Rate * 100)} [{N(cell.Candidate.ClearRate.Lower * 100)}, {N(cell.Candidate.ClearRate.Upper * 100)}] | {N(cell.ClearRateChange!.MeanChange)} |");
        text.AppendLine($"\nAll {report.UnchangedOpeningControlCells} opening-area control cells preserve gameplay exactly. Control suites contain {report.Controls.Values.Sum(c => c.Cells.Count(x => x.GameplayChanges > 0))} changed cells overall; inspect their paired reports for later-area effects. {report.Replays.Count} preselected detailed replays matched.\n");
        text.AppendLine("The axis also changes enemy health regeneration through the existing square-root scaling rule. It affects multiple Shenic areas; areaEffects in results.json records offense and regeneration ratios. Health, defenses, abilities and build recipes are fixed. Intervals are pointwise; discovery selection is exploratory and reserved confirmation is not pooled with it. No observed failure or success authorizes automatic production promotion. Keep plan, captured sources/fixtures, selection, runs, comparisons, evaluations and replays together.");
        return text.ToString();
    }
}
