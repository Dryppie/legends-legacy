using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerPracticalSearch
{
    public const string ThreeReferenceVersion = "tower-practical-three-reference-search-v1";
    public const string ThreeReferenceAllocationVersion = "tower-practical-three-reference-allocated-search-v1";
    internal static bool IsAllocatedVersion(string version) => version is AllocationVersion or ThreeReferenceAllocationVersion;
    internal static bool IsDeclaredVersion(string version) => version is Version or ThreeReferenceVersion;
    internal static string ResultVersion(TowerBossDiscoveryDefinition d)
        => TowerSuppliedCompositionSearch.HasThreeReferences(d.Generation.PolicyVersion) ? ThreeReferenceVersion : Version;
    private static string ResultVersion(TowerPracticalRequest q)
        => q.Version is ThreeReferenceVersion or ThreeReferenceAllocationVersion ? ThreeReferenceVersion : Version;

    internal static void ValidateRequestDefinition(TowerPracticalRequest q, TowerBossDiscoveryDefinition d)
        => Require((q.Version is ThreeReferenceVersion or ThreeReferenceAllocationVersion)
            == TowerSuppliedCompositionSearch.HasThreeReferences(d.Generation.PolicyVersion),
            "The three-reference request and generation versions must be declared together; legacy requests retain two references.");

    /// <summary>Import an explicitly selected recipe and retain both exact existing controls. No implicit primary designation.</summary>
    internal static TowerBossDiscoveryDefinition ApplyThreeReferencePreset(TowerPracticalRequest q,
        TowerBossDiscoveryDefinition source, JsonElement bundle, LoadoutScope scope, string evidenceHash)
    {
        Require(q.Version == AllocationVersion && source.Generation.PolicyVersion == TowerSuppliedCompositionSearch.IncumbentVersion,
            "Start from a newly declared unscheduled two-reference incumbent request.");
        ValidateAllocationTemplate(q, source);
        Require(bundle.GetProperty("version").GetString() == "tower-confirmed-team-reuse-v1"
            && source.SettingsHash == HarnessJson.Hash(scope.Settings)
            && HarnessJson.Hash(source.ContentHashes) == HarnessJson.Hash(scope.ContentHashes),
            "Reuse content and effective settings must match the intended template.");
        var selected = bundle.GetProperty("selectedPartyId").GetString()!;
        var controls = bundle.GetProperty("controlPartyIds").Deserialize<string[]>(HarnessJson.Options)!;
        var teams = bundle.GetProperty("teams").EnumerateArray().ToArray();
        Require(TowerContractJson.Hash(selected) && controls.Length == 2 && controls.Distinct().Count() == 2
            && !controls.Contains(selected) && teams.Length == 3
            && teams.Select(t => t.GetProperty("partyId").GetString()).ToHashSet().SetEquals(controls.Append(selected)),
            "Reuse requires one explicit selected recipe and both distinct controls.");
        var referenceIds = source.Starts.ToDictionary(s => s.Party.Id, s => s.ReferenceId);
        Require(referenceIds.Keys.ToHashSet().SetEquals(controls), "Keep both original reference controls; do not replace either.");
        TowerScenario Scenario(JsonElement team) => team.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!;
        foreach (var id in controls)
        {
            var team = teams.Single(t => t.GetProperty("partyId").GetString() == id);
            Require(team.GetProperty("control").GetBoolean()
                && HarnessJson.Hash(Scenario(team)) == HarnessJson.Hash(source.References.Single(r => r.Id == referenceIds[id]).Scenario),
                "The reuse controls must preserve the exact supplied scenarios.");
        }
        var candidate = teams.Single(t => t.GetProperty("partyId").GetString() == selected);
        var scenario = Scenario(candidate);
        var party = TowerPartySelection.Choice("confirmed-supplied", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
        Require(candidate.GetProperty("qualifies").GetBoolean() && !candidate.GetProperty("control").GetBoolean()
            && party.Id == selected && scenario.Seeds.Count == 0, "Selected reuse recipe is not an exact seed-free qualifier.");
        var idNew = "confirmed-" + selected[..24];
        Require(source.References.All(r => r.Id != idNew), "New reference ID collides with an existing control.");
        var result = source with {
            Generation = source.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.ThreeReferenceVersion },
            Stages = source.Stages with { Shortlist = 5 },
            References = source.References.Append(new BossBenchmarkReference(idNew, source.Contexts.Single().Id, scenario,
                "Explicit confirmed-team reuse; historical fitness is not a current measurement", evidenceHash)).ToArray(),
            Starts = source.Starts.Append(new BossDiscoveryStart("start-" + HarnessJson.Hash(idNew)[..24], idNew, party)).ToArray()
        };
        // Actual caller budgets, pool, ownership, identities and schedules govern admission.
        // A five-nominee/four-recipe envelope must already fit; never increase it here.
        ValidateAllocationTemplate(q with { Version = ThreeReferenceAllocationVersion }, result);
        return result;
    }

    public static object CreateThreeReferencePreset(string sourceRequestPath, string reuseRoot, string manifestHash,
        string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preset preparation cannot fight.")).Activate();
        sourceRequestPath = Path.GetFullPath(sourceRequestPath); reuseRoot = Path.GetFullPath(reuseRoot); output = Path.GetFullPath(output);
        Unlinked(sourceRequestPath); Unlinked(reuseRoot); Unlinked(Path.GetDirectoryName(output)!);
        var sourceHash = HarnessJson.FileHash(sourceRequestPath);
        var q = TowerContractJson.Read<TowerPracticalRequest>(sourceRequestPath); ValidateRequest(q);
        Require(TowerContractJson.Hash(manifestHash) && !Path.Exists(output) && !Path.Exists(q.OutputRoot)
            && !Inside(output, q.RegistryRoot) && !Inside(output, q.ContentRoot) && !Inside(output, reuseRoot)
            && !Inside(reuseRoot, output), "Use a pinned reuse bundle and new, separate preset/search outputs.");
        void CheckSource()
        {
            token.ThrowIfCancellationRequested();
            Require(HarnessJson.FileHash(sourceRequestPath) == sourceHash && HarnessJson.FileHash(q.DefinitionPath) == q.DefinitionHash
                && HarnessJson.FileHash(Path.Combine(reuseRoot, "files.json")) == manifestHash && !Path.Exists(q.OutputRoot),
                "Preset source changed or search output already exists.");
            TowerBulkCampaign.VerifyFiles(reuseRoot, "files.json", true, token);
        }
        CheckSource();
        var receipt = HarnessJson.Read<JsonElement>(Path.Combine(reuseRoot, "reuse.json"));
        var bundle = HarnessJson.Read<JsonElement>(Path.Combine(reuseRoot, "teams.json"));
        Require(receipt.GetProperty("version").GetString() == "tower-confirmed-team-reuse-v1"
            && receipt.GetProperty("status").GetString() == "ReadyForExplicitReuse"
            && receipt.GetProperty("selectedPartyId").GetString() == bundle.GetProperty("selectedPartyId").GetString()
            && HarnessJson.Hash(receipt.GetProperty("controlPartyIds")) == HarnessJson.Hash(bundle.GetProperty("controlPartyIds")),
            "Missing matching successful reuse receipt.");
        var template = ApplyThreeReferencePreset(q, TowerBossDiscovery.Read(q.DefinitionPath), bundle,
            HarnessJson.Read<LoadoutScope>(Path.Combine(reuseRoot, "scope.json")), manifestHash);
        var converted = q with { Version = ThreeReferenceAllocationVersion };
        var cost = TowerBossDiscovery.Validate(ValidateAllocationTemplate(converted, template));
        using var lease = TowerCompactBundle.AcquireWriter(output);
        Require(!Path.Exists(output), "Preset output already exists.");
        CheckSource(); Directory.CreateDirectory(output);
        var templatePath = Path.Combine(output, "template.json");
        HarnessJson.WriteNew(templatePath, template);
        var request = converted with { DefinitionPath = templatePath, DefinitionHash = HarnessJson.FileHash(templatePath) };
        ValidateRequest(request); HarnessJson.WriteNew(Path.Combine(output, "request.json"), request); CheckSource();
        var prepared = new { version = ThreeReferenceAllocationVersion, status = "PreparedNeedsAdmission", sourceRequestHash = sourceHash,
            reuseManifestHash = manifestHash, selectedPartyId = bundle.GetProperty("selectedPartyId").GetString(),
            referenceIds = template.References.Select(r => r.Id).ToArray(), templateHash = request.DefinitionHash,
            requestHash = HarnessJson.FileHash(Path.Combine(output, "request.json")), cost, intervalFamily = 10,
            admissionRequired = true, fights = 0, newValues = 0 };
        HarnessJson.WriteNew(Path.Combine(output, "preset.json"), prepared);
        return prepared;
    }
}
