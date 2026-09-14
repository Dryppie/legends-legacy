using System.Text.Json;

namespace BalanceHarness;

/// <summary>Reconstruct sealed source artifacts before executing a separate, non-resumable precision campaign.</summary>
public static class TowerPrecisionBalanceRun
{
    public const string Kind = "tower-composite-precision-v1";

    public static TowerPrecisionSource VerifySource(string output, string expectedManifestHash,
        CancellationToken token = default, Action<string>? progress = null)
    {
        const string manifest = "campaign-manifest.json";
        if (!TowerContractJson.Hash(expectedManifestHash) || HarnessJson.FileHash(Path.Combine(output, manifest)) != expectedManifestHash)
            throw new InvalidDataException("Source campaign manifest differs from the frozen precision reference.");
        TowerBulkCampaign.VerifyFiles(output, manifest, exact: true, token);
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, TowerBulkCampaign.ContractFile));
        var d = TowerStagedBalance.Read(Path.Combine(output, "definition.json"));
        if (contract.SchemaVersion != 1 || contract.Kind != TowerStagedBalanceRun.Kind
            || HarnessJson.Hash(contract.Definition) != HarnessJson.Hash(d)
            || HarnessJson.Hash(contract.Scope.ContentHashes) != HarnessJson.Hash(d.ContentHashes)
            || HarnessJson.Hash(contract.Scope.Settings) != d.SettingsHash || HarnessJson.Hash(contract.Scope.Execution) != d.ExecutionHash
            || contract.PlannedBattles != TowerStagedBalance.Validate(d))
            throw new InvalidDataException("Source campaign contract differs from its complete staged definition.");
        var batch = 0; var charged = 0;
        List<TowerBalanceEvidence> Reconstruct(IReadOnlyList<TowerStagedCell> cells, int stage)
        {
            var evidence = new List<TowerBalanceEvidence>(); var sources = new List<TowerBalanceRunSource>();
            foreach (var chunk in cells.Chunk(32))
            {
                token.ThrowIfCancellationRequested();
                var id = "confirmation-" + (batch++).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                var path = Path.Combine(output, "batches", id);
                var cases = chunk.Select((c, i) => new TowerCompactCase("cell-" + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture),
                    TowerStagedBalance.Scenario(d, c, stage))).ToArray();
                var expected = new TowerCompactDefinition(1, id, cases.Sum(c => c.Scenario.Seeds.Count) + contract.Options.RetryReserve,
                    contract.Options.ChunkSize, cases);
                if (HarnessJson.Hash(TowerCompactBundle.Definition(path)) != HarnessJson.Hash(expected))
                    throw new InvalidDataException("Source batch differs from deterministic full-family reconstruction.");
                var saved = TowerCompactBundle.Verify(path, token);
                if (saved.Plan.SharedContentPath != "../../content" || saved.Plan.ExecutionMode != contract.Options.ExecutionMode
                    || HarnessJson.Hash(saved.Scope.Settings) != d.SettingsHash || HarnessJson.Hash(saved.Scope.Execution) != d.ExecutionHash
                    || HarnessJson.Hash(saved.Scope.ContentHashes) != HarnessJson.Hash(d.ContentHashes))
                    throw new InvalidDataException("Source batch content/settings/producing execution differs.");
                charged = checked(charged + TowerCompactBundle.AttemptCount(path));
                for (var i = 0; i < chunk.Length; i++)
                {
                    evidence.Add(TowerCompactBundle.Evidence(chunk[i].Id, saved, cases[i].Id));
                    sources.Add(new(chunk[i].Id, "batches/" + id, cases[i].Id));
                }
                progress?.Invoke($"Precision source: stage {stage}, {evidence.Count}/{cells.Count} cells reconstructed; zero combats.");
            }
            if (HarnessJson.Hash(evidence) != HarnessJson.Hash(HarnessJson.Read<List<TowerBalanceEvidence>>(Path.Combine(output, $"stage-{stage}-evidence.json")))
                || HarnessJson.Hash(sources) != HarnessJson.Hash(HarnessJson.Read<List<TowerBalanceRunSource>>(Path.Combine(output, $"stage-{stage}-sources.json"))))
                throw new InvalidDataException("Source evidence or source index differs from reconstructed archives.");
            return evidence;
        }
        var anchors = d.AnchorIds.ToHashSet(StringComparer.Ordinal);
        var first = Reconstruct(d.Cells.Where(c => !anchors.Contains(c.Id)).ToArray(), 1);
        var selection = TowerStagedBalance.Select(d, first);
        if (selection.Status != "Proceed" || HarnessJson.Hash(selection) != HarnessJson.Hash(
            TowerContractJson.Read<TowerStagedSelection>(Path.Combine(output, "stage-selection.json"))))
            throw new InvalidDataException("Source selection is incomplete or differs from reconstruction.");
        var selected = selection.SecondStageIds.ToHashSet(StringComparer.Ordinal);
        var second = Reconstruct(d.Cells.Where(c => selected.Contains(c.Id)).ToArray(), 2);
        var report = TowerStagedBalance.Evaluate(d, first, second);
        var accounting = TowerContractJson.Read<TowerBulkAccounting>(Path.Combine(output, "campaign-accounting.json"));
        if (HarnessJson.Hash(report) != HarnessJson.Hash(TowerContractJson.Read<TowerStagedReport>(Path.Combine(output, "assessment.json")))
            || File.ReadAllText(Path.Combine(output, "assessment.md")) != TowerStagedBalance.Markdown(report)
            || !Directory.GetDirectories(Path.Combine(output, "batches")).Select(Path.GetFileName).Order(StringComparer.Ordinal)
                .SequenceEqual(Enumerable.Range(0, batch).Select(i => "confirmation-" + i.ToString("D6", System.Globalization.CultureInfo.InvariantCulture)))
            || accounting.LogicalTrials != report.LogicalTrials || accounting.ChargedAttempts != charged
            || accounting.RetryOrUncommittedAttempts != charged - report.LogicalTrials || charged < report.LogicalTrials
            || accounting.MaximumAttempts != contract.MaximumAttempts || charged > contract.MaximumAttempts)
            throw new InvalidDataException("Source assessment, batch inventory or attempt accounting differs.");
        return new(d, first, second);
    }

    public static async Task<TowerPrecisionReport> RunAsync(string root, string output, TowerPrecisionDefinition definition,
        string sourceOutput, JsonElement ledger, TowerBulkOptions options, CancellationToken token = default,
        Action<string>? progress = null, bool verifyOnly = false)
    {
        var d = JsonSerializer.Deserialize<TowerPrecisionDefinition>(JsonSerializer.SerializeToUtf8Bytes(definition, HarnessJson.Options), HarnessJson.Options)!;
        var frozenLedger = ledger.Clone();
        if (options.RetryReserve != 0) throw new InvalidDataException("Precision has no retries or optional continuation.");
        var source = VerifySource(sourceOutput, d.SourceManifestHash, token, progress);
        var selection = TowerPrecisionBalance.Validate(d, source, frozenLedger);
        var sourceExecution = TowerContractJson.Read<TowerBulkContract>(Path.Combine(sourceOutput, TowerBulkCampaign.ContractFile)).Scope.Execution;
        var current = ExecutionIdentity.Current();
        // Harness-only analysis changes are allowed; gameplay assemblies and runtime must remain identical.
        if (sourceExecution.Runtime != current.Runtime || sourceExecution.OperatingSystem != current.OperatingSystem
            || sourceExecution.Architecture != current.Architecture || HarnessJson.Hash(sourceExecution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary())
                != HarnessJson.Hash(current.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary()))
            throw new InvalidDataException("Precision requires unchanged gameplay assemblies and runtime.");
        using var campaign = TowerBulkCampaign.Open(root, output, Kind, d, source.Definition.ContentHashes,
            source.Definition.SettingsHash, d.ExecutionHash, d.MaximumBattles, d.MaximumBattles, options,
            resume: verifyOnly, verifyOnly: verifyOnly, token, progress);
        var ids = d.FreshCellIds.ToHashSet(StringComparer.Ordinal);
        var cells = source.Definition.Cells.Where(c => ids.Contains(c.Id)).OrderBy(c => c.Id, StringComparer.Ordinal).ToArray();
        var runner = new TowerBattleRunner(campaign.Root, new OfflineContent(campaign.Root, campaign.Contract.Scope.Settings.Threat));
        foreach (var c in cells)
            runner.CreateInput(TowerPrecisionBalance.Scenario(d, c), d.FreshSeeds[0], campaign.Contract.Scope.Settings.Threat, campaign.Contract.Scope.Settings.CheckpointIntervalTicks);
        try
        {
            campaign.Result("source-ledger.json", frozenLedger);
            campaign.Result("precision-selection.json", selection);
            var evidence = new List<TowerBalanceEvidence>(); var sources = new List<TowerBalanceRunSource>(); var batch = 0;
            foreach (var chunk in cells.Chunk(32))
            {
                var id = "confirmation-" + (batch++).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                var cases = chunk.Select((c, i) => new TowerCompactCase("cell-" + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), TowerPrecisionBalance.Scenario(d, c))).ToArray();
                var saved = await campaign.BatchAsync(id, cases);
                for (var i = 0; i < chunk.Length; i++)
                {
                    evidence.Add(TowerCompactBundle.Evidence(chunk[i].Id, saved, cases[i].Id));
                    sources.Add(new(chunk[i].Id, "batches/" + id, cases[i].Id));
                }
            }
            var report = TowerPrecisionBalance.Evaluate(d, source, frozenLedger, evidence);
            campaign.Result("evidence.json", evidence); campaign.Result("sources.json", sources);
            campaign.Result("assessment.json", report); campaign.TextResult("assessment.md", TowerPrecisionBalance.Markdown(report));
            campaign.Finish(report.FreshTrials);
            return report;
        }
        catch (Exception error)
        {
            campaign.Failure(error is OperationCanceledException ? "Cancelled" : "Invalid", error.Message); throw;
        }
    }

    public static Task<TowerPrecisionReport> VerifyAsync(string output, string sourceOutput, CancellationToken token = default, Action<string>? progress = null)
    {
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, TowerBulkCampaign.ContractFile));
        if (contract.Kind != Kind) throw new InvalidDataException("Not a composite precision campaign.");
        return RunAsync(Path.Combine(output, "content"), output, contract.Definition.Deserialize<TowerPrecisionDefinition>(HarnessJson.Options)!,
            sourceOutput, HarnessJson.Read<JsonElement>(Path.Combine(output, "source-ledger.json")), contract.Options, token, progress, verifyOnly: true);
    }
}
