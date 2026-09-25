namespace BalanceHarness;

public sealed record TowerPracticalPresetReceipt(string Version, string Status,
    string SourceRequestPath, string SourceRequestFileHash, string SourceTemplateFileHash,
    string RequestPath, string RequestFileHash, string TemplatePath, string TemplateFileHash,
    string SelectionPolicyVersion, string SelectionPrimaryReferenceId, string SelectionPrimaryPartyId,
    BossDiscoveryCost Cost, bool AdmissionRequired, int NewValues, int Fights);

public static partial class TowerPracticalSearch
{
    public const string IncumbentTiePresetVersion = "tower-practical-incumbent-tie-preset-v1";

    internal static TowerBossDiscoveryDefinition ApplyIncumbentTiePreset(TowerPracticalRequest q,
        TowerBossDiscoveryDefinition source, string primaryReferenceId)
    {
        Require(q.Version == AllocationVersion && q.Allocation is not null,
            "The incumbent-tie preset requires an allocated-search request and an unscheduled template.");
        var stages = source.Stages ?? throw new InvalidDataException("Missing selection stages.");
        Require(source.Mode == TowerBossDiscovery.Improve
            && source.Generation?.PolicyVersion == TowerSuppliedCompositionSearch.IncumbentVersion
            && stages.SelectionPolicyVersion is TowerBossStudyPolicy.ZeroWinVersion or TowerBossStudyPolicy.IncumbentTieVersion,
            "The preset preserves the incumbent generator; other generators or selectors require their own explicit configuration.");
        Require(!string.IsNullOrWhiteSpace(primaryReferenceId)
            && (stages.SelectionPrimaryReferenceId is null || stages.SelectionPrimaryReferenceId == primaryReferenceId),
            "Name the exact supplied reference; an existing selection designation cannot be replaced by this preset.");
        // Reuse the caller's actual allocation counts and existing shape/cost checks.
        // Temporary validation labels are never derived, exported or reserved.
        ValidateAllocationTemplate(q, source);
        var result = source with { Stages = stages with {
            SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
            SelectionPrimaryReferenceId = primaryReferenceId } };
        ValidateAllocationTemplate(q, result);
        return result;
    }

    /// <summary>Prepare new input files only. Admission, allocation and execution remain separate existing commands.</summary>
    public static TowerPracticalPresetReceipt CreateIncumbentTiePreset(string sourceRequestPath,
        string primaryReferenceId, string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preset preparation cannot fight.")).Activate();
        sourceRequestPath = Path.GetFullPath(sourceRequestPath); output = Path.GetFullPath(output);
        Unlinked(sourceRequestPath);
        var sourceHash = HarnessJson.FileHash(sourceRequestPath);
        var q = TowerContractJson.Read<TowerPracticalRequest>(sourceRequestPath);
        ValidateRequest(q);
        Require(!Path.Exists(q.OutputRoot), "The requested search output already exists; prepare a newly declared search.");
        Require(!Path.Exists(output) && !Inside(output, q.RegistryRoot) && !Inside(output, q.ContentRoot),
            "Use a new preset directory outside the history registry and content root.");
        Unlinked(Path.GetDirectoryName(output)!);
        Require(HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Source template differs from its request pin.");
        var template = ApplyIncumbentTiePreset(q, TowerBossDiscovery.Read(q.DefinitionPath), primaryReferenceId);
        var cost = TowerBossDiscovery.Validate(ValidateAllocationTemplate(q, template));
        using var lease = TowerCompactBundle.AcquireWriter(output);
        Require(!Path.Exists(output), "Preset output already exists; source files are never overwritten.");
        void CheckSource()
        {
            token.ThrowIfCancellationRequested();
            Require(HarnessJson.FileHash(sourceRequestPath) == sourceHash
                && HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash, "Preset source changed during preparation.");
            Require(!Path.Exists(q.OutputRoot), "Search output appeared during preset preparation.");
        }
        CheckSource();
        Directory.CreateDirectory(output);
        var templatePath = Path.Combine(output, "template.json");
        var requestPath = Path.Combine(output, "request.json");
        HarnessJson.WriteNew(templatePath, template);
        var templateHash = HarnessJson.FileHash(templatePath);
        var request = q with { DefinitionPath = templatePath, DefinitionHash = templateHash };
        ValidateRequest(request);
        HarnessJson.WriteNew(requestPath, request);
        CheckSource();
        var receipt = new TowerPracticalPresetReceipt(IncumbentTiePresetVersion, "PreparedNeedsAdmission",
            sourceRequestPath, sourceHash, q.DefinitionHash, requestPath, HarnessJson.FileHash(requestPath),
            templatePath, templateHash, template.Stages.SelectionPolicyVersion, primaryReferenceId,
            template.Starts.Single(s => s.ReferenceId == primaryReferenceId).Party.Id, cost, true, 0, 0);
        // Publish the receipt last. A partial directory has no successful preparation receipt.
        HarnessJson.WriteNew(Path.Combine(output, "preset.json"), receipt);
        return receipt;
    }
}
