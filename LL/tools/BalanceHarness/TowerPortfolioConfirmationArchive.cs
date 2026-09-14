using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

internal static class TowerPortfolioConfirmationArchive
{
    internal static readonly string[] SourceFiles = ["definition.json", "comparison.json", "shortlist.json", "selected.json", "controls.json",
        "seed-ledger.json", "discovery/campaign.json", "protocol.json", "summary.json"];
    internal static readonly string[] WorkFiles = ["final-verification.json", "independent-selection-check.json", "verification.json", "family.json"];

    internal static TowerPortfolioConfirmationSource Read(string root) => new(
        TowerBossDiscovery.Read(Path.Combine(root, "definition.json")), HarnessJson.Read<TowerFeedbackComparison>(Path.Combine(root, "comparison.json")),
        HarnessJson.Read<TowerFeedbackShortlist>(Path.Combine(root, "shortlist.json")), HarnessJson.Read<TowerFeedbackSelection>(Path.Combine(root, "selected.json")),
        HarnessJson.Read<TowerSearchSelected[]>(Path.Combine(root, "controls.json")));

    internal static void VerifySources(string run, string work, CancellationToken token)
    {
        TowerBulkCampaign.VerifyFiles(run, "final-files.json", true, token);
        TowerBulkCampaign.VerifyFiles(work, "work-files.json", true, token);
        ValidateBindings(run, work, Path.Combine(run, "final-files.json"), Path.Combine(work, "work-files.json"));
        if (Path.Exists(Path.Combine(run, "confirmation")) || Path.Exists(Path.Combine(run, "confirmation-definition.json")))
            throw new InvalidDataException("The source must retain its capacity stop without confirmation.");
        TowerRescreenAttempts.Verify(Path.Combine(run, "attempts.bin"), 86016);
    }

    internal static void Copy(string run, string work, string output)
    {
        foreach (var name in SourceFiles) CopyFile(Path.Combine(run, name), Path.Combine(output, "source", name));
        foreach (var name in WorkFiles.Append("work-files.json")) CopyFile(Path.Combine(work, name), Path.Combine(output, "source-audit", name));
        CopyFile(Path.Combine(run, "final-files.json"), Path.Combine(output, "source-audit/source-files.json"));
        ValidateImported(output);
    }
    private static void CopyFile(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(source, destination);
    }
    internal static void ValidateImported(string output) => ValidateBindings(Path.Combine(output, "source"), Path.Combine(output, "source-audit"),
        Path.Combine(output, "source-audit/source-files.json"), Path.Combine(output, "source-audit/work-files.json"));

    private static void ValidateBindings(string run, string work, string runManifest, string workManifest)
    {
        var files = HarnessJson.Read<Dictionary<string, string>>(runManifest); var auditFiles = HarnessJson.Read<Dictionary<string, string>>(workManifest);
        foreach (var name in SourceFiles)
            if (!files.TryGetValue(name, out var hash) || HarnessJson.FileHash(Path.Combine(run, name)) != hash)
                throw new InvalidDataException("Imported v19 artifact differs: " + name);
        foreach (var name in WorkFiles)
            if (!auditFiles.TryGetValue(name, out var hash) || HarnessJson.FileHash(Path.Combine(work, name)) != hash)
                throw new InvalidDataException("Imported audit artifact differs: " + name);
        var receipt = HarnessJson.Read<JsonElement>(Path.Combine(work, "final-verification.json"));
        var selection = HarnessJson.Read<JsonElement>(Path.Combine(work, "independent-selection-check.json"));
        var verification = HarnessJson.Read<JsonElement>(Path.Combine(work, "verification.json"));
        var export = HarnessJson.Read<JsonElement>(Path.Combine(work, "family.json"));
        var summary = HarnessJson.Read<TowerFeedbackSummary>(Path.Combine(run, "summary.json"));
        var source = Read(run); TowerPortfolioConfirmation.ValidateSource(source);
        var protocol = HarnessJson.Read<TowerFeedbackProtocol>(Path.Combine(run, "protocol.json"));
        if (receipt.GetProperty("status").GetString() != "VerifiedCapacityExceeded"
            || receipt.GetProperty("campaignManifestHash").GetString() != HarnessJson.FileHash(runManifest)
            || receipt.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(Path.Combine(run, "protocol.json"))
            || receipt.GetProperty("independentSelectionHash").GetString() != HarnessJson.FileHash(Path.Combine(work, "independent-selection-check.json"))
            || receipt.GetProperty("verificationHash").GetString() != HarnessJson.FileHash(Path.Combine(work, "verification.json"))
            || receipt.GetProperty("requiredFamilyRecipes").GetInt32() != 253 || receipt.GetProperty("confirmationFights").GetInt32() != 0
            || receipt.GetProperty("totalReservedSeeds").GetInt32() != 480707 || receipt.GetProperty("reliability").GetString() != "Unresolved"
            || receipt.GetProperty("adoption").GetString() != "Hold" || selection.GetProperty("status").GetString() != "Pass"
            || selection.GetProperty("requiredFamilyRecipes").GetInt32() != 253 || !selection.GetProperty("exactOrigins").GetBoolean()
            || selection.GetProperty("newCombats").GetInt32() != 0 || selection.GetProperty("newSeeds").GetInt32() != 0
            || verification.GetProperty("newFights").GetInt32() != 0 || verification.GetProperty("exitCode").GetInt32() != 0
            || summary.Status != "CapacityExceeded" || summary.Started != 86016 || summary.Completed != 86016 || summary.ConfirmationRecipes != 253 || summary.Quality is not null
            || source.Comparison.AnchorId != TowerPortfolioConfirmation.Anchor || source.Comparison.StrongControlId != TowerPortfolioConfirmation.StrongControl
            || protocol.Version != TowerSearchPortfolio.Policy || protocol.MaximumFights != TowerSearchPortfolio.MaximumFights || protocol.MaximumSeconds != TowerSearchPortfolio.MaximumSeconds
            || protocol.MaximumBytes != TowerSearchPortfolio.MaximumBytes || protocol.CombatRetries != 0
            || protocol.AnchorId != source.Comparison.AnchorId || protocol.StrongControlId != source.Comparison.StrongControlId)
            throw new InvalidDataException("The complete sealed capacity-stop and independent-selection receipts are required.");
        TowerPortfolioConfirmation.Equal(source.Comparison.Family, export.GetProperty("family"), "complete family export and origins");
        if (export.GetProperty("sourceManifestHash").GetString() != HarnessJson.FileHash(runManifest)) throw new InvalidDataException("Family export scope differs.");
        var history = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(run, "seed-ledger.json")));
        TowerPortfolioConfirmation.Equal(TowerPortfolioConfirmation.SourceHistory(source), history, "all source reservations");
        if (history.Length != 480707) throw new InvalidDataException("The complete 480,707-value exclusion ledger is required.");
    }

    internal static void ValidateScope(string content, string sourceRoot, TowerPortfolioConfirmationSource source, CancellationToken token)
    {
        TowerPortfolioConfirmation.ValidateSource(source);
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(sourceRoot, "discovery/campaign.json"));
        var scope = contract.Scope;
        RequireGameplay(scope.Execution, ExecutionIdentity.Current());
        TowerPortfolioConfirmation.Equal(source.Definition, contract.Definition, "source discovery definition");
        TowerPortfolioConfirmation.Equal(source.Definition.ExecutionHash, HarnessJson.Hash(scope.Execution), "source gameplay identity");
        TowerPortfolioConfirmation.Equal(source.Definition.ContentHashes, scope.ContentHashes, "source content");
        TowerPortfolioConfirmation.Equal(source.Definition.SettingsHash, HarnessJson.Hash(scope.Settings), "source settings");
        TowerPortfolioConfirmation.Equal(scope.Settings, TowerBundle.ReadSettings(content), "captured settings");
        TowerPortfolioConfirmation.Equal(scope.ContentHashes, TowerCompactBundle.ContentHashes(content, token), "captured content");
        foreach (var compatible in RecipeScopes(source, HarnessJson.Hash(ExecutionIdentity.Current())))
        {
            token.ThrowIfCancellationRequested();
            TowerBossDiscovery.Validate(content, compatible);
        }
    }

    // Production materialization is bounded by the unchanged discovery reference limit. Statistical cells are never split.
    internal static IEnumerable<TowerBossDiscoveryDefinition> RecipeScopes(TowerPortfolioConfirmationSource source, string executionHash) =>
        source.Comparison.Family.Chunk(112).Select(cells => source.Definition with { ExecutionHash = executionHash,
            References = cells.Select(c => new BossBenchmarkReference(c.Id, source.Definition.Contexts[0].Id,
                c.Scenario, "Frozen complete v19 family", HarnessJson.Hash(c))).ToArray() });

    internal static void RequireGameplay(ExecutionIdentity saved, ExecutionIdentity current)
    {
        if (saved.Runtime != current.Runtime || saved.OperatingSystem != current.OperatingSystem || saved.Architecture != current.Architecture)
            throw new InvalidDataException("Captured runtime/OS/architecture differs.");
        TowerPortfolioConfirmation.Equal(saved.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key).ToDictionary(),
            current.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key).ToDictionary(), "captured gameplay DLLs");
    }

    internal static object Audit(string run, string work, string content, string output, string? comparisonContent, string? comparisonExecutable, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); output = Path.GetFullPath(output);
        if (Path.Exists(output)) throw new IOException("Choose a new audit output.");
        using var lease = TowerCompactBundle.AcquireWriter(output);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(900));
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Version auditing cannot fight."));
        using var active = trace.Activate(); var watch = Stopwatch.StartNew();
        try
        {
        VerifySources(run, work, timeout.Token); var source = Read(run); ValidateScope(content, run, source, timeout.Token);
        var scope = TowerContractJson.Read<TowerBulkContract>(Path.Combine(run, "discovery/campaign.json")).Scope;
        var contentDiff = comparisonContent is null ? [] : scope.ContentHashes.Select(p => {
            var path = Path.Combine(comparisonContent, "Data", p.Key); var current = File.Exists(path) ? HarnessJson.FileHash(path) : null;
            return new { path = p.Key, saved = p.Value, current, identical = current == p.Value };
        }).ToArray();
        var assemblyDiff = comparisonExecutable is null ? [] : scope.Execution.AssemblyHashes.Select(p => {
            var path = Path.Combine(comparisonExecutable, p.Key + ".dll"); var current = File.Exists(path) ? HarnessJson.FileHash(path) : null;
            return new { assembly = p.Key, saved = p.Value, current, identical = current == p.Value };
        }).ToArray();
        var result = new { status = "CapturedScopeCompatible", newFights = 0, newSeeds = 0, recipes = source.Comparison.Family.Count,
            nominationGroups = source.Selection.Arms.Count, retainedControls = source.Controls.Count, reservedSeeds = 480707, unusedConfirmationSeeds = 512,
            sourceManifestHash = HarnessJson.FileHash(Path.Combine(run, "final-files.json")), workManifestHash = HarnessJson.FileHash(Path.Combine(work, "work-files.json")),
            familyHash = HarnessJson.Hash(source.Comparison.Family), selectionHash = HarnessJson.Hash(source.Selection), capturedScope = scope,
            intendedExecution = ExecutionIdentity.Current(), comparisonContent = contentDiff, comparisonAssemblies = assemblyDiff,
            comparisonSettingsIdentical = comparisonContent is null ? (bool?)null : HarnessJson.Hash(TowerBundle.ReadSettings(comparisonContent)) == HarnessJson.Hash(scope.Settings),
            seconds = watch.Elapsed.TotalSeconds, timings = trace.Snapshot(), confirmationPrepared = false, confirmationFights = 0,
            scope = "Retain captured v19 gameplay for the original reliability comparison. Updated gameplay needs a separate scope/baseline." };
        Directory.CreateDirectory(output); Copy(run, work, output);
        HarnessJson.WriteNew(Path.Combine(output, "audit.json"), result);
        return result;
        }
        catch (Exception e)
        {
            Directory.CreateDirectory(output);
            HarnessJson.WriteNew(Path.Combine(output, "audit-failure.json"), new { error = e.ToString(), seconds = watch.Elapsed.TotalSeconds,
                newFights = 0, newSeeds = 0, timings = trace.Snapshot() });
            throw;
        }
    }
}
