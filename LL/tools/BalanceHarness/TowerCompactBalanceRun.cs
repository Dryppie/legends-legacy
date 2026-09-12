using System.Text.Json;

namespace BalanceHarness;

/// <summary>Execute an already-frozen calibration/confirmation family, then use the unchanged 10–50% evaluator.</summary>
public static class TowerCompactBalanceRun
{
    public const string Kind = "tower-compact-balance-v1";

    public static async Task<TowerBalanceReport> RunAsync(string root, string output, TowerBalanceDefinition definition,
        TowerBulkOptions options, bool resume = false, CancellationToken token = default, Action<string>? progress = null,
        bool verifyOnly = false)
    {
        var d = JsonSerializer.Deserialize<TowerBalanceDefinition>(JsonSerializer.SerializeToUtf8Bytes(definition, HarnessJson.Options), HarnessJson.Options)!;
        TowerBalanceEvaluator.Validate(d);
        using var campaign = TowerBulkCampaign.Open(root, output, Kind, d, d.ContentHashes, d.SettingsHash, d.ExecutionHash,
            d.Cells.Sum(c => c.Scenario.Seeds.Count), d.MaximumBattles, options, resume, verifyOnly, token, progress);
        // Check the entire family before any fight, including cells assigned to later batches.
        var runner = new TowerBattleRunner(campaign.Root, new OfflineContent(campaign.Root, campaign.Contract.Scope.Settings.Threat));
        foreach (var cell in d.Cells)
        {
            campaign.Token.ThrowIfCancellationRequested();
            runner.CreateInput(cell.Scenario, cell.Scenario.Seeds[0], campaign.Contract.Scope.Settings.Threat, campaign.Contract.Scope.Settings.CheckpointIntervalTicks);
        }
        var evidence = new List<TowerBalanceEvidence>(); var sources = new List<TowerBalanceRunSource>(); var batch = 0;
        try
        {
            foreach (var cells in d.Cells.Chunk(32))
            {
                var id = "confirmation-" + (batch++).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                // Internal ordinal case IDs also support the evaluator's longer user-authored cell IDs.
                var cases = cells.Select((c, i) => new TowerCompactCase("cell-" + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), c.Scenario)).ToArray();
                var saved = await campaign.BatchAsync(id, cases);
                for (var index = 0; index < cells.Length; index++)
                {
                    evidence.Add(TowerCompactBundle.Evidence(cells[index].Id, saved, cases[index].Id));
                    sources.Add(new(cells[index].Id, "batches/" + id, cases[index].Id));
                }
            }
            var report = TowerBalanceEvaluator.Evaluate(d, evidence);
            campaign.Result("sources.json", sources);
            campaign.Result("evidence.json", evidence);
            campaign.Result("assessment.json", report);
            campaign.TextResult("assessment.md", TowerBalanceEvaluator.Markdown(report));
            campaign.Finish(evidence.Sum(e => e.Trials.Count));
            return report;
        }
        catch (Exception error)
        {
            campaign.Failure(error is OperationCanceledException ? "Cancelled" : "Invalid", error.Message);
            throw;
        }
    }

    public static Task<TowerBalanceReport> VerifyAsync(string output, CancellationToken token = default)
    {
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, TowerBulkCampaign.ContractFile));
        if (contract.Kind != Kind) throw new InvalidDataException("Not a compact balance campaign.");
        return RunAsync(Path.Combine(output, "content"), output, contract.Definition.Deserialize<TowerBalanceDefinition>(HarnessJson.Options)!,
            contract.Options, resume: true, token: token, verifyOnly: true);
    }
}
