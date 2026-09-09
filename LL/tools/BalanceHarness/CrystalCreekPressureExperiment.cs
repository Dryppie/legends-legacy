using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record CreekPressureCandidate(string Id, double Barrier, double IceNeedle);
public enum CreekPressureSweep { Coarse, Fine }
public sealed record CreekPressurePlan(string Id, int Samples, int ConfirmationSamples, int DiscoverySeed,
    int ConfirmationSeed, int PlannedBattles, IReadOnlyList<CreekPressureCandidate> Candidates,
    string FixtureHash, string GoalsHash, string SettingsHash, string ExecutionHash,
    IReadOnlyDictionary<string, string> SourceHashes, IReadOnlyDictionary<string, string> FixtureHashes,
    string SelectionRule, string StoppingRule, string ReplaySelection,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int DiscoverySamples = 0)
{
    [JsonIgnore] public int EffectiveDiscoverySamples => DiscoverySamples == 0 ? Samples : DiscoverySamples;
}
public sealed record CreekPressureFinding(CreekPressureCandidate Candidate, IReadOnlyList<CellScorecard> Cells,
    double MaximumDeviation, double MeanDeviation, string ArtifactHash);
public sealed record CreekPressureReport(string Status, int ValidBattles, CreekPressureFinding Selected,
    IReadOnlyList<CreekPressureFinding> Discovery, GoalEvaluationReport Confirmation,
    ComparisonReport HandoffComparison, IReadOnlyDictionary<string, ComparisonReport> Controls,
    int UnchangedControlCells, IReadOnlyList<string> Replays);

/// <summary>A fixed two-axis creature-only content experiment; no production mutation or automatic promotion.</summary>
public static class CrystalCreekPressureExperiment
{
    public const string Goals = "idle-crystal-creek-starter-goals.json";
    public const string AbilityFile = "combat/abilities.json";
    public const string MappingFile = "combat/creature-abilities.json";
    public const string BarrierAbility = "ability.creature.blue_slime.protective_slime";
    public const string NeedleAbility = "ability.creature.frost_imp.ice_needle";
    public const string VariantSuffix = ".balance_creek";
    private const string CreekArea = "region_01_area_03";
    private static readonly string[] Controls = ["idle-reference.json", "idle-first-hunt.json"];
    private static readonly string[] Fixtures = [CrystalCreekHandoff.Fixture, Goals, .. Controls];
    private static readonly double[] Barriers = [0.07, 0.14, 0.21, 0.28, 0.35, 0.42, 0.56];
    private static readonly double[] Needles = [1.6, 2.4, 3.2, 4.8, 6.4, 8.0, 9.6];
    private static readonly double[] FineBarriers = [0.35, 0.38, 0.41, 0.44, 0.47, 0.50];
    private static readonly double[] FineNeedles = [1.7, 1.8, 1.9, 2.0, 2.1, 2.2, 2.3];
    public static CreekPressureCandidate OriginalCandidate { get; } = new("barrier-07-needle-016", 0.07, 1.6);

    public static CreekPressurePlan CreatePlan(string root, string fixtureRoot, int samples, CreekPressureSweep sweep = CreekPressureSweep.Coarse)
    {
        if (samples is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(samples));
        if (!Enum.IsDefined(sweep)) throw new ArgumentOutOfRangeException(nameof(sweep));
        var fine = sweep == CreekPressureSweep.Fine;
        _ = BloodGroveLocalValidation.ReadCandidateCatalog(root);
        var handoff = CrystalCreekHandoff.CreatePlan(root, Path.Combine(fixtureRoot, CrystalCreekHandoff.Fixture), samples);
        var goals = BalanceGoals.Read(Path.Combine(fixtureRoot, Goals));
        if (handoff.FixtureHash != "63a44203a7c787d7f0627205a56dd7ff91775bc17a9873bfa38e671d536e41ec"
            || HarnessJson.Hash(goals) != "e3112d21b47fcbbdcd860b140b8e250c83ce1608e45604e6e979a10280ab70f0")
            throw new InvalidDataException("The reviewed checkpoint or policy changed; declare a new protocol.");
        var abilities = HarnessJson.Read<JsonArray>(Path.Combine(root, "Data", AbilityFile));
        if (abilities.Any(a => a!["id"]!.GetValue<string>() is var id
                && (id == BarrierAbility + VariantSuffix || id == NeedleAbility + VariantSuffix)))
            throw new InvalidDataException("Historical pressure protocols require original creature content; use the retained source-content directory or an explicitly restored content copy.");
        if (Ability(abilities, BarrierAbility)["effects"]![0]!["scalingCoefficient"]!.GetValue<double>() != 0.07
            || Ability(abilities, NeedleAbility)["effects"]![0]!["scalingCoefficient"]!.GetValue<double>() != 1.6)
            throw new InvalidDataException("The source creature ability values changed.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        var content = new OfflineContent(root, threat);
        // Resolve actual seeds, without executing or observing reserved outcomes.
        int[] seeds = fine ? [818091, 818092, 818093, 818095, 718091, 718092, 718093, 718094, 618091, 618092, 618093, 618094]
            : [718091, 718092, 718093, 718094, 618091, 618092, 618093, 618094];
        var schedules = seeds.Select(seed => IdleSuite.Resolve(handoff.Suite with
            { SamplesPerCell = seed < 700000 ? 500 : seed < 800000 ? 1000 : 2000 }, content, threat, cadence, seed)
            .Cells.SelectMany(c => c.Trials.Select(t => t.Seed)).ToHashSet()).ToArray();
        for (var i = 0; i < schedules.Length; i++)
        for (var j = i + 1; j < schedules.Length; j++)
            if (schedules[i].Overlaps(schedules[j])) throw new InvalidDataException("Reserved trial schedules overlap.");
        var controlCells = Controls.Sum(file => IdleSuite.Resolve(HarnessJson.Read<IdleSuiteDefinition>(
            Path.Combine(fixtureRoot, file)) with { SamplesPerCell = 1 }, content, threat, cadence, 718094).Cells.Count);
        if (controlCells != 48) throw new InvalidDataException("Control fixture coverage changed.");
        var candidates = (from barrier in (fine ? FineBarriers : Barriers) from needle in (fine ? FineNeedles : Needles)
            select new CreekPressureCandidate(FormattableString.Invariant($"barrier-{barrier * 100:00}-needle-{needle * 10:000}"), barrier, needle)).ToArray();
        return new(fine ? "crystal-creek-creature-pressure-fine-v1" : "crystal-creek-creature-pressure-v1", samples, samples * (fine ? 20 : 10),
            fine ? (samples == 100 ? 818091 : 818093) : (samples == 100 ? 718091 : 718093),
            fine ? (samples == 100 ? 818092 : 818095) : (samples == 100 ? 718092 : 718094), samples * (fine ? 2752 : 1200),
            candidates, handoff.FixtureHash, HarnessJson.Hash(goals), handoff.SettingsHash, handoff.ExecutionHash,
            handoff.SourceHashes, Fixtures.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(fixtureRoot, f))),
            "Minimize worst primary-cell distance from 70%, then mean distance, then summed relative coefficient increase, then ID. Freeze selection before confirmation; ignore duration and diagnostic outcomes.",
            $"Run all {candidates.Length} candidates once, then original/selected confirmation and both control cohorts. No refinement, replacement selection, pooling, sample extension or promotion. Preserve all non-Creek control gameplay and Blood Grove returns.",
            "Confirmation trial index 0 in both primary cells for original and selected content: four detailed replays, independent of outcomes.",
            fine ? samples * 3 : 0);
    }

    private static JsonNode Ability(JsonArray abilities, string id) => abilities.Single(a => a!["id"]!.GetValue<string>() == id)!;

    public static void CreateContentCopy(string source, string output, CreekPressureCandidate candidate, CancellationToken token = default)
    {
        if (!(Barriers.Contains(candidate.Barrier) || FineBarriers.Contains(candidate.Barrier))
            || !(Needles.Contains(candidate.IceNeedle) || FineNeedles.Contains(candidate.IceNeedle)))
            throw new ArgumentOutOfRangeException(nameof(candidate));
        BloodGrovePressureExperiment.CreateContentCopy(source, output, 2.3, token);
        var abilities = HarnessJson.Read<JsonArray>(Path.Combine(output, "Data", AbilityFile));
        var mapping = HarnessJson.Read<JsonNode>(Path.Combine(output, "Data", MappingFile));
        var changed = false;
        Clone("monster.blue_slime", BarrierAbility, candidate.Barrier, 0.07);
        Clone("monster.frost_imp", NeedleAbility, candidate.IceNeedle, 1.6);
        if (!changed) return;
        File.WriteAllText(Path.Combine(output, "Data", AbilityFile), abilities.ToJsonString(HarnessJson.Options));
        File.WriteAllText(Path.Combine(output, "Data", MappingFile), mapping.ToJsonString(HarnessJson.Options));

        void Clone(string monster, string originalId, double coefficient, double originalCoefficient)
        {
            var ids = mapping["creatures"]!.AsArray().Single(c => c!["monsterId"]!.GetValue<string>() == monster)!["abilityIds"]!.AsArray();
            var index = ids.Select((id, index) => (Id: id!.GetValue<string>(), Index: index))
                .Single(x => x.Id == originalId || x.Id == originalId + VariantSuffix).Index;
            var previous = abilities.SingleOrDefault(a => a!["id"]!.GetValue<string>() == originalId + VariantSuffix);
            if (previous is not null)
            {
                abilities.Remove(previous);
                ids[index] = originalId;
                changed = true;
            }
            else if (ids[index]!.GetValue<string>() != originalId)
                throw new InvalidDataException("Creature variant mapping has no matching ability.");
            if (coefficient == originalCoefficient) return;
            changed = true;
            var variant = Ability(abilities, originalId).DeepClone();
            variant["id"] = originalId + VariantSuffix;
            // Creature variants must not become extra slots in player Essence catalogs or simulator rosters.
            variant.AsObject().Remove("owningEssenceId");
            foreach (var effect in variant["effects"]!.AsArray())
                effect!["id"] = effect["id"]!.GetValue<string>() + VariantSuffix;
            variant["effects"]![0]!["scalingCoefficient"] = coefficient;
            if (originalId == NeedleAbility)
                variant["description"] = FormattableString.Invariant($"Deal {coefficient * 100:0}% Magical Damage to a random enemy.");
            abilities.Add(variant);
            ids[index] = originalId + VariantSuffix;
        }
    }

    public static CreekPressureFinding SelectCandidate(IEnumerable<CreekPressureFinding> findings) => findings
        .OrderBy(f => f.MaximumDeviation).ThenBy(f => f.MeanDeviation)
        .ThenBy(f => f.Candidate.Barrier / 0.07 + f.Candidate.IceNeedle / 1.6 - 2)
        .ThenBy(f => f.Candidate.Id, StringComparer.Ordinal).First();

    public static async Task<CreekPressureReport> RunAsync(string root, string fixtureRoot, string outputDirectory,
        int samples, CancellationToken token, Action<string>? progress = null, CreekPressureSweep sweep = CreekPressureSweep.Coarse)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        token.ThrowIfCancellationRequested();
        var plan = CreatePlan(root, fixtureRoot, samples, sweep);
        Directory.CreateDirectory(output);
        try
        {
            HarnessJson.WriteNew(Path.Combine(output, "plan.json"), plan);
            var capturedFixtures = Path.Combine(output, "fixtures");
            Directory.CreateDirectory(capturedFixtures);
            foreach (var file in Fixtures) File.Copy(Path.Combine(fixtureRoot, file), Path.Combine(capturedFixtures, file));
            var source = Path.Combine(output, "source-content");
            CreateContentCopy(root, source, OriginalCandidate, token);
            foreach (var file in plan.SourceHashes.Keys.Except(OfflineContent.Files))
            {
                var target = Path.Combine(source, "Data", file);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(root, "Data", file), target);
            }
            VerifySource(root, fixtureRoot, plan);
            VerifySource(source, capturedFixtures, plan);
            var expected = new Dictionary<string, IReadOnlyDictionary<string, string>>();
            var roots = new Dictionary<string, string>();
            // Validate and hash every content copy before the first discovery battle.
            var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(capturedFixtures, CrystalCreekHandoff.Fixture));
            var goals = BalanceGoals.Read(Path.Combine(capturedFixtures, Goals));
            var (threat, cadence) = RunBundle.ReadCombatSettings(source);
            expected.Add(source, OfflineContent.Files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(source, "Data", f))));
            foreach (var candidate in plan.Candidates)
            {
                token.ThrowIfCancellationRequested();
                var candidateRoot = candidate == OriginalCandidate ? source : Path.Combine(output, "candidates", candidate.Id);
                if (candidateRoot != source) CreateContentCopy(source, candidateRoot, candidate, token);
                roots.Add(candidate.Id, candidateRoot);
                if (candidateRoot != source) expected.Add(candidateRoot, OfflineContent.Files.ToDictionary(f => f,
                    f => HarnessJson.FileHash(Path.Combine(candidateRoot, "Data", f))));
                _ = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, new OfflineContent(candidateRoot, threat), threat, cadence, plan.DiscoverySeed);
            }
            HarnessJson.WriteNew(Path.Combine(output, "candidate-content-hashes.json"), plan.Candidates.ToDictionary(c => c.Id, c => expected[roots[c.Id]]));
            var valid = 0;
            async Task<SavedSuite> Run(string contentRoot, string fixture, string relative, int seed, int count)
            {
                token.ThrowIfCancellationRequested();
                Verify();
                progress?.Invoke($"{relative}: {count} trials per cell");
                Verify(); // callbacks may cancel or edit inputs; never consume reserved trials after a change.
                token.ThrowIfCancellationRequested();
                var run = Path.Combine(output, relative);
                var result = await SuiteBundle.CreateAsync(contentRoot, Path.Combine(capturedFixtures, fixture), run, seed, count, token);
                if (result.Status != "Complete")
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidDataException($"Incomplete pressure run: {relative}");
                }
                var saved = SavedSuite.Read(run, token);
                if (HarnessJson.Hash(saved.Manifest.Execution) != plan.ExecutionHash
                    || expected[contentRoot].Any(p => saved.Manifest.ContentHashes[p.Key] != p.Value))
                    throw new InvalidDataException("Archived content or execution differs from the frozen plan.");
                Verify();
                valid += result.Valid;
                return saved;

                void Verify()
                {
                    VerifySource(source, capturedFixtures, plan);
                    var (currentThreat, currentCadence) = RunBundle.ReadCombatSettings(contentRoot);
                    if (HarnessJson.Hash(new { threat = currentThreat, cadence = currentCadence }) != plan.SettingsHash
                        || expected[contentRoot].Any(p => HarnessJson.FileHash(Path.Combine(contentRoot, "Data", p.Key)) != p.Value))
                        throw new InvalidDataException("Candidate content or settings changed after preflight.");
                }
            }
            ComparisonReport Compare(SavedSuite before, SavedSuite after, string relative)
            {
                var baseline = Path.Combine(output, relative, "control-baseline.json");
                BaselineManifest.Accept(before.Directory, baseline, "Experimental original-content control only; not a passing gameplay baseline.", token);
                var comparison = SuiteComparison.Create(baseline, after.Directory, Path.Combine(output, relative, "comparison"), token);
                if (comparison.Status != "Complete") throw new InvalidDataException("Controlled runs are incompatible.");
                foreach (var cell in before.Input.Cells.Where(c => c.Input.Scenario.AreaId != CreekArea))
                    if (comparison.Cells.Single(c => c.CellId == cell.Id).GameplayChanges != 0)
                        throw new InvalidDataException($"Non-Creek control gameplay changed: {cell.Id}.");
                return comparison;
            }
            var findings = new List<CreekPressureFinding>();
            foreach (var candidate in plan.Candidates)
            {
                var saved = await Run(roots[candidate.Id], CrystalCreekHandoff.Fixture, "discovery/" + candidate.Id, plan.DiscoverySeed, plan.EffectiveDiscoverySamples);
                var primary = saved.Scorecard.Cells.Where(c => goals.RequiredCells.Contains(c.CellId)).ToArray();
                var deviations = primary.Select(c => Math.Abs(c.ClearRate!.Rate * 100 - 70)).ToArray();
                findings.Add(new(candidate, primary, deviations.Max(), deviations.Average(), saved.ArtifactHash));
            }
            var selected = SelectCandidate(findings);
            HarnessJson.WriteNew(Path.Combine(output, "selection.json"), selected);
            progress?.Invoke($"Selection frozen: {selected.Candidate.Id}; worst distance from 70%: {selected.MaximumDeviation:0.0} pp.");
            var original = await Run(source, CrystalCreekHandoff.Fixture, "confirmation/original", plan.ConfirmationSeed, plan.ConfirmationSamples);
            var chosen = await Run(roots[selected.Candidate.Id], CrystalCreekHandoff.Fixture, "confirmation/candidate", plan.ConfirmationSeed, plan.ConfirmationSamples);
            var goalPath = Path.Combine(capturedFixtures, Goals);
            _ = GoalEvaluationBundle.Create(goalPath, original.Directory, null, Path.Combine(output, "evaluation/original"), token);
            var assessment = GoalEvaluationBundle.Create(goalPath, chosen.Directory, null, Path.Combine(output, "evaluation/candidate"), token);
            var handoffComparison = Compare(original, chosen, "handoff-comparison");
            var comparisons = new Dictionary<string, ComparisonReport>();
            var unchanged = 0;
            foreach (var fixture in Controls)
            {
                var id = Path.GetFileNameWithoutExtension(fixture);
                var before = await Run(source, fixture, $"controls/{id}/original", plan.ConfirmationSeed, samples);
                var after = await Run(roots[selected.Candidate.Id], fixture, $"controls/{id}/candidate", plan.ConfirmationSeed, samples);
                comparisons.Add(id, Compare(before, after, "controls/" + id));
                unchanged += before.Input.Cells.Count(c => c.Input.Scenario.AreaId != CreekArea);
            }
            var replays = new List<string>();
            Directory.CreateDirectory(Path.Combine(output, "replays"));
            foreach (var saved in new[] { original, chosen })
            foreach (var cell in saved.Input.Cells.Where(c => goals.RequiredCells.Contains(c.Id)))
            {
                var replay = await SuiteBundle.ReplayAsync(saved.Directory, cell.Trials[0].BattleId, true, token);
                var relative = $"replays/{Path.GetFileName(saved.Directory)}-{cell.Trials[0].BattleId}.json";
                HarnessJson.WriteNew(Path.Combine(output, relative), replay);
                replays.Add(relative);
            }
            VerifySource(root, fixtureRoot, plan);
            VerifySource(source, capturedFixtures, plan);
            if (valid != plan.PlannedBattles || unchanged != 32 || replays.Count != 4)
                throw new InvalidDataException("Incomplete battle budget or control/replay coverage.");
            var report = new CreekPressureReport("Complete", valid, selected, findings, assessment, handoffComparison, comparisons, unchanged, replays);
            HarnessJson.WriteNew(Path.Combine(output, "results.json"), report);
            await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), Markdown(report), token);
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            { Status = exception is OperationCanceledException ? "Cancelled" : "Invalid", ErrorType = exception.GetType().Name, exception.Message });
            throw;
        }
    }

    private static void VerifySource(string root, string fixtureRoot, CreekPressurePlan plan)
    {
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        if (plan.SourceHashes.Any(p => HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) != p.Value)
            || plan.FixtureHashes.Any(p => HarnessJson.FileHash(Path.Combine(fixtureRoot, p.Key)) != p.Value)
            || HarnessJson.Hash(new { threat, cadence }) != plan.SettingsHash
            || HarnessJson.Hash(ExecutionIdentity.Current()) != plan.ExecutionHash)
            throw new InvalidDataException("Frozen sources, fixtures, settings or execution identity changed.");
    }

    private static string Markdown(CreekPressureReport report)
    {
        static string N(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
        var text = new StringBuilder($"# Crystal Creek creature pressure\n\n{report.Status}: {report.ValidBattles} valid battles. Reserved confirmation policy: **{report.Confirmation.GateStatus}**.\n\n");
        text.AppendLine($"Selection: {report.Selected.Candidate.Id}. All {report.UnchangedControlCells} non-Creek controls and eight Blood Grove returns stayed identical; four fixed replays matched. No production content or passing baseline was promoted.\n");
        text.AppendLine("| Discovery candidate | Two Slimes win % | Slime + Imp win % | Worst distance from 70 pp |\n| --- | --- | --- | --- |");
        foreach (var finding in report.Discovery)
            text.AppendLine($"| {finding.Candidate.Id} | {N(finding.Cells[0].ClearRate!.Rate * 100)} | {N(finding.Cells[1].ClearRate!.Rate * 100)} | {N(finding.MaximumDeviation)} |");
        text.AppendLine("\nRead evaluation/candidate/evaluation.md for separate confirmation intervals and diagnostic build scorecards in confirmation/candidate. Discovery and confirmation are not pooled. Duration does not gate this policy. The copied creature abilities do not replace player Essence abilities. Other activity modes and the full natural spawn distribution remain unmeasured. Keep the complete plan, content copies, selection, runs, comparisons, evaluations and replays together.");
        return text.ToString();
    }
}
