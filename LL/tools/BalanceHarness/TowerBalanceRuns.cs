using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerBalanceRunSource(string CellId, string RunDirectory);

/// <summary>Adapt verified normal Tower archives to the evaluator; never rerun combat or accept self-reported counts.</summary>
public static class TowerBalanceRuns
{
    public static TowerBalanceEvidence Read(string cellId, string directory, CancellationToken token = default)
    {
        try
        {
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
        catch (Exception error) when (error is InvalidDataException or IOException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return new(cellId, "Invalid", "", "", "", "", 0, [], "", $"Could not verify Tower archive: {error.Message}");
        }
    }

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
        var observations = resolvedSources.Select(s => Read(s.CellId, s.RunDirectory, token)).ToArray();
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
