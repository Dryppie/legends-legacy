using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerBalanceRunSource(string CellId, string RunDirectory,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? CompactCaseId = null);

/// <summary>Adapt verified normal Tower archives to the evaluator; never rerun combat or accept self-reported counts.</summary>
public static class TowerBalanceRuns
{
    public static TowerBalanceEvidence Read(string cellId, string directory, CancellationToken token = default, string? compactCaseId = null)
    {
        using var timing = TowerPerformanceTrace.Measure("archive.balance-verify");
        try
        {
            if (File.Exists(Path.Combine(directory, "bulk-plan.json")) || File.Exists(Path.Combine(directory, TowerCompactBundle.ManifestFile)))
                return TowerCompactBundle.Evidence(cellId, TowerCompactBundle.Verify(directory, token), compactCaseId);
            if (compactCaseId is not null) throw new InvalidDataException("A compact case ID cannot select a normal Tower archive.");
            var saved = TowerBundle.ReadSaved(directory, token);
            var first = saved.Inputs[0];
            var contentRoot = Path.Combine(directory, "content");
            var runner = new TowerBattleRunner(contentRoot, new OfflineContent(contentRoot, first.ThreatAndTanking));
            var expected = runner.CreateInput(first.Scenario, first.Rules.RandomSeed, first.ThreatAndTanking, first.CheckpointIntervalTicks);
            // Reconstruct the seed-independent preparation once. The remaining inputs
            // must match it exactly, apart from their declared random seed.
            foreach (var input in saved.Inputs)
            {
                token.ThrowIfCancellationRequested();
                var template = expected with { Rules = expected.Rules with { RandomSeed = input.Rules.RandomSeed } };
                if (HarnessJson.Hash(template) != HarnessJson.Hash(input))
                    throw new InvalidDataException("Saved Tower preparation or rules differ from the frozen production recipe.");
            }
            return new(cellId, saved.Scorecard.Status, HarnessJson.Hash(first.Scenario), HarnessJson.Hash(saved.Manifest.ContentHashes),
                HarnessJson.Hash(new TowerSettings(first.ThreatAndTanking, first.CheckpointIntervalTicks)),
                HarnessJson.Hash(saved.Manifest.Execution), first.Floor.RequiredSlots,
                saved.Scorecard.Trials.Select(t => new TowerBalanceTrial(t.Seed, t.Report.Battle.Summary.ContentOutcome)).ToArray(),
                HarnessJson.Hash(new { saved.Manifest, Results = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(directory, "tower-results.json")) }));
        }
        catch (Exception error) when (VerificationError(error))
        {
            return Invalid(cellId, error);
        }
    }

    internal static IReadOnlyList<TowerBalanceEvidence> ReadSources(IReadOnlyList<TowerBalanceRunSource> sources, CancellationToken token = default)
    {
        var results = new TowerBalanceEvidence[sources.Count];
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        foreach (var group in sources.Select((source, index) => (Source: source, Index: index)).GroupBy(s => Path.GetFullPath(s.Source.RunDirectory), comparer))
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(Path.Combine(group.Key, "bulk-plan.json")) && !File.Exists(Path.Combine(group.Key, TowerCompactBundle.ManifestFile)))
            {
                foreach (var item in group) results[item.Index] = Read(item.Source.CellId, group.Key, token, item.Source.CompactCaseId);
                continue;
            }
            // Share one verified snapshot within this evaluation only. Do not retain full reports across bundles or calls.
            VerifiedTowerCompact saved;
            try { saved = TowerCompactBundle.Verify(group.Key, token); }
            catch (Exception error) when (VerificationError(error))
            {
                foreach (var item in group) results[item.Index] = Invalid(item.Source.CellId, error);
                continue;
            }
            foreach (var item in group)
            {
                token.ThrowIfCancellationRequested();
                try { results[item.Index] = TowerCompactBundle.Evidence(item.Source.CellId, saved, item.Source.CompactCaseId); }
                catch (Exception error) when (VerificationError(error)) { results[item.Index] = Invalid(item.Source.CellId, error); }
            }
        }
        return results;
    }

    private static bool VerificationError(Exception error) => error is InvalidDataException or IOException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException;
    private static TowerBalanceEvidence Invalid(string cellId, Exception error) =>
        new(cellId, "Invalid", "", "", "", "", 0, [], "", $"Could not verify Tower archive: {error.Message}");

    public static TowerBalanceReport Evaluate(string definitionPath, string sourcesPath, string output, CancellationToken token = default)
    {
        var definition = TowerBalanceEvaluator.Read(definitionPath);
        TowerBalanceEvaluator.Validate(definition);
        var sources = TowerContractJson.Read<TowerBalanceRunSource[]>(sourcesPath);
        if (sources.Any(s => s is null || string.IsNullOrWhiteSpace(s.CellId) || string.IsNullOrWhiteSpace(s.RunDirectory)))
            throw new InvalidDataException("Every archive mapping requires a cell ID and run directory.");
        if (Path.Exists(output)) throw new IOException("Choose a new Tower balance report directory.");
        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcesPath))!;
        var resolvedSources = sources.Select(s => s with { RunDirectory = Path.GetFullPath(s.RunDirectory, baseDirectory) }).ToArray();
        var observations = ReadSources(resolvedSources, token);
        var report = TowerBalanceEvaluator.Evaluate(definition, observations);
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
        HarnessJson.WriteNew(Path.Combine(output, "sources.json"), resolvedSources.Select(s => s with {
            RunDirectory = Path.GetRelativePath(Path.GetFullPath(output), s.RunDirectory) }).ToArray());
        HarnessJson.WriteNew(Path.Combine(output, "evidence.json"), observations);
        HarnessJson.WriteNew(Path.Combine(output, "assessment.json"), report);
        File.WriteAllText(Path.Combine(output, "assessment.md"), TowerBalanceEvaluator.Markdown(report));
        return report;
    }
}
