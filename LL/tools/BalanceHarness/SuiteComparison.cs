using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record EvidenceChange(string Kind, string Name, string? Baseline, string? Candidate);
public sealed record ChangedBattle(string BattleId, int Seed, BattleOutcome BaselineOutcome,
    BattleOutcome CandidateOutcome, double BaselineSeconds, double CandidateSeconds,
    double BaselineHealthFraction, double CandidateHealthFraction);
public sealed record CellComparison(string CellId, string Status, IReadOnlyList<string> Reasons,
    CellScorecard? Baseline, CellScorecard? Candidate, int Pairs = 0, int GainedWins = 0, int LostWins = 0,
    int OutcomeChanges = 0, int GameplayChanges = 0, PairedEstimate? ClearRateChange = null,
    PairedEstimate? SharedWinDurationChange = null, PairedEstimate? RemainingHealthChange = null,
    IReadOnlyList<ChangedBattle>? Examples = null);
public sealed record ComparisonReport(int SchemaVersion, string ComparisonVersion, string Policy, string Status,
    string BaselineManifest, string BaselineReason, string BaselineRun, string CandidateRun,
    string BaselineArtifactHash, string CandidateArtifactHash, string CandidateStatus,
    IReadOnlyList<EvidenceChange> EvidenceChanges, IReadOnlyList<CellComparison> Cells);

public static class SuiteComparison
{
    public const string Version = "idle-paired-comparison-v1";

    public static ComparisonReport Create(string baselineFile, string candidateDirectory, string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Comparison output already exists: {output}");
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        try
        {
            var (manifest, baseline) = BaselineManifest.Read(baselineFile, cancellationToken);
            var candidate = SavedSuite.Read(candidateDirectory, cancellationToken);
            var report = Compare(baseline, candidate, Path.GetFullPath(baselineFile), manifest.Reason, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            HarnessJson.WriteNew(Path.Combine(output, "comparison.json"), report);
            File.WriteAllText(Path.Combine(output, "comparison.md"), ComparisonMarkdown.Render(report));
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            {
                Status = exception is OperationCanceledException ? "Cancelled" : "Invalid",
                ErrorType = exception.GetType().Name, exception.Message
            });
            throw;
        }
    }

    public static ComparisonReport Compare(SavedSuite baseline, SavedSuite candidate,
        string baselineFile, string reason, CancellationToken cancellationToken = default)
    {
        if (baseline.Scorecard.Status != "Complete") throw new InvalidDataException("Baseline must be complete.");
        var changes = new List<EvidenceChange>();
        void Changed(string kind, string name, string? before, string? after)
        {
            if (before != after) changes.Add(new(kind, name, before, after));
        }
        void ChangedMap(string kind, IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
        {
            foreach (var key in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
                Changed(kind, key, before.GetValueOrDefault(key), after.GetValueOrDefault(key));
        }
        ChangedMap("Content", baseline.Manifest.ContentHashes, candidate.Manifest.ContentHashes);
        ChangedMap("Assembly", baseline.Manifest.Execution.AssemblyHashes, candidate.Manifest.Execution.AssemblyHashes);
        Changed("Environment", "Runtime", baseline.Manifest.Execution.Runtime, candidate.Manifest.Execution.Runtime);
        Changed("Environment", "Operating system", baseline.Manifest.Execution.OperatingSystem, candidate.Manifest.Execution.OperatingSystem);
        Changed("Environment", "Architecture", baseline.Manifest.Execution.Architecture, candidate.Manifest.Execution.Architecture);
        Changed("Suite", "Definition", HarnessJson.Hash(baseline.Input.Definition), HarnessJson.Hash(candidate.Input.Definition));
        var globalReasons = new List<string>();
        if (baseline.Input.Definition.Id != candidate.Input.Definition.Id) globalReasons.Add("Suite identity changed.");
        if (baseline.Input.SeedScheduleVersion != candidate.Input.SeedScheduleVersion
            || baseline.Input.MasterSeed != candidate.Input.MasterSeed) globalReasons.Add("Seed schedule or master seed changed.");
        if (changes.Any(c => c.Kind == "Environment")) globalReasons.Add("Runtime/platform changed; use matching environments for a controlled comparison.");

        var beforeCells = baseline.Input.Cells.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var afterCells = candidate.Input.Cells.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var beforeScores = baseline.Scorecard.Cells.ToDictionary(c => c.CellId, StringComparer.Ordinal);
        var afterScores = candidate.Scorecard.Cells.ToDictionary(c => c.CellId, StringComparer.Ordinal);
        var cells = new List<CellComparison>();
        foreach (var id in beforeCells.Keys.Union(afterCells.Keys).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = beforeCells.GetValueOrDefault(id);
            var after = afterCells.GetValueOrDefault(id);
            var beforeScore = beforeScores.GetValueOrDefault(id);
            var afterScore = afterScores.GetValueOrDefault(id);
            if (before is null || after is null)
            {
                cells.Add(new(id, before is null ? "Added" : "Removed", ["No matching cell in the other run."], beforeScore, afterScore));
                continue;
            }
            var problems = new List<string>(globalReasons);
            if (HarnessJson.Hash(ScenarioContract(before)) != HarnessJson.Hash(ScenarioContract(after)))
                problems.Add("Scenario, acquisition assumptions or loadout recipe changed.");
            if (HarnessJson.Hash(CharacterContract(before)) != HarnessJson.Hash(CharacterContract(after)))
                problems.Add("Materialized character level, equipment selections or essence progression changed.");
            if (HarnessJson.Hash(before.Input.Rules with { RandomSeed = 0, CaptureEventLog = false })
                != HarnessJson.Hash(after.Input.Rules with { RandomSeed = 0, CaptureEventLog = false })
                || before.Input.EncounterCadenceSeconds != after.Input.EncounterCadenceSeconds)
                problems.Add("Combat rules or encounter cadence changed.");
            if (HarnessJson.Hash(before.Trials.OrderBy(t => t.Index)) != HarnessJson.Hash(after.Trials.OrderBy(t => t.Index)))
                problems.Add("Trial count, identities or seeds changed; partial seed overlap is not used.");
            if (baseline.TickRates.TryGetValue(id, out var beforeRate) && candidate.TickRates.TryGetValue(id, out var afterRate)
                && beforeRate != afterRate) problems.Add("Tick-rate units changed.");
            Changed("Resolved input", id + " / character", HarnessJson.Hash(before.Input.Character), HarnessJson.Hash(after.Input.Character));
            Changed("Resolved input", id + " / creature", HarnessJson.Hash(before.Input.Creature), HarnessJson.Hash(after.Input.Creature));
            Changed("Resolved input", id + " / additional creatures", HarnessJson.Hash(before.Input.AdditionalCreatures), HarnessJson.Hash(after.Input.AdditionalCreatures));
            Changed("Resolved input", id + " / area", HarnessJson.Hash(before.Input.Area), HarnessJson.Hash(after.Input.Area));
            Changed("Combat settings", id + " / threat", HarnessJson.Hash(before.Input.ThreatAndTanking), HarnessJson.Hash(after.Input.ThreatAndTanking));
            if (problems.Count > 0)
            {
                cells.Add(new(id, "Incompatible", problems, beforeScore, afterScore));
                continue;
            }
            if (afterScore!.Valid != afterScore.Planned)
            {
                cells.Add(new(id, "Incomplete", ["Invalid, cancelled or unexecuted trials; no subset comparison is made."], beforeScore, afterScore));
                continue;
            }
            var pairs = before.Trials.OrderBy(t => t.Index).Select(trial =>
                (Before: baseline.Observations[trial.BattleId], After: candidate.Observations[trial.BattleId])).ToArray();
            var gained = pairs.Count(p => p.Before.Outcome != BattleOutcome.Victory && p.After.Outcome == BattleOutcome.Victory);
            var lost = pairs.Count(p => p.Before.Outcome == BattleOutcome.Victory && p.After.Outcome != BattleOutcome.Victory);
            var changed = pairs.Where(p => baseline.GameplayHashes[p.Before.BattleId] != candidate.GameplayHashes[p.After.BattleId]).ToArray();
            var examples = changed.OrderByDescending(p => p.Before.Outcome != p.After.Outcome)
                .ThenByDescending(p => Math.Abs(p.After.DurationSeconds!.Value - p.Before.DurationSeconds!.Value))
                .ThenByDescending(p => Math.Abs(p.After.RemainingHealthFraction!.Value - p.Before.RemainingHealthFraction!.Value))
                .ThenBy(p => p.Before.BattleId, StringComparer.Ordinal).Take(3)
                .Select(p => new ChangedBattle(p.Before.BattleId, p.Before.Seed, p.Before.Outcome!.Value, p.After.Outcome!.Value,
                    p.Before.DurationSeconds!.Value, p.After.DurationSeconds!.Value,
                    p.Before.RemainingHealthFraction!.Value, p.After.RemainingHealthFraction!.Value)).ToArray();
            cells.Add(new(id, "Compared", [], beforeScore, afterScore, pairs.Length, gained, lost,
                pairs.Count(p => p.Before.Outcome != p.After.Outcome), changed.Length,
                PairedStatistics.ClearRate(gained, lost, pairs.Length),
                PairedStatistics.Mean(pairs.Where(p => p.Before.Outcome == BattleOutcome.Victory && p.After.Outcome == BattleOutcome.Victory)
                    .Select(p => p.After.DurationSeconds!.Value - p.Before.DurationSeconds!.Value), "seconds"),
                PairedStatistics.Mean(pairs.Select(p => 100 * (p.After.RemainingHealthFraction!.Value - p.Before.RemainingHealthFraction!.Value)), "percentage points"),
                examples));
        }
        return new(1, Version, "Advisory", candidate.Scorecard.Status == "Complete" && cells.All(c => c.Status == "Compared")
                ? "Complete" : "Incomplete", baselineFile, reason, baseline.Directory, candidate.Directory,
            baseline.ArtifactHash, candidate.ArtifactHash, candidate.Scorecard.Status, changes, cells);
    }

    private static object ScenarioContract(SuiteCell cell)
    {
        var scenario = cell.Input.Scenario;
        return scenario with { Build = scenario.Build is { } build
            ? build with { Equipment = build.Equipment.OrderBy(e => e.Slot).ToArray() } : null };
    }

    // Derived coefficients may change while testing a content/engine update. The player's
    // progression and selections must remain fixed, and all resolved changes are surfaced.
    private static object CharacterContract(SuiteCell cell) => new
    {
        cell.Input.Character.Level, cell.Input.Character.Essences,
        Equipment = cell.Input.Character.Equipment.OrderBy(e => e.Slot).Select(e => new
        {
            e.Slot, e.Data.State.DefinitionId, e.Data.State.Tier, e.Data.State.Rank,
            e.Data.State.Quality, e.Data.State.AttributeRollMultiplier, e.Data.State.ActiveStyleId
        }).ToArray()
    };
}
