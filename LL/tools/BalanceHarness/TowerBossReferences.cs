using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerBossReferenceOrigin(string Method, int GenerationSeed, string ProposalSource);
public sealed record TowerBossReferenceHistoricalObservation(string PackageId, TowerBossReferenceEvidence Evidence);
public sealed record TowerBossReferenceEvidence(string Id, string Label, string OriginalSource, bool WasPilotControl,
    IReadOnlyList<string> StrategyLabels, IReadOnlyList<TowerBossReferenceOrigin> DiscoveryOrigins,
    TowerScenario TargetRecipe, string TargetRecipeHash, BossMeasurement Confirmation,
    IReadOnlyDictionary<string, string> SourceHashes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? WasDiscoveryPrimary = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerBossReferenceHistoricalObservation>? PriorObservations = null);
public sealed record TowerBossReferenceProvenance(string PackageId, string ManifestSha256, string Interpretation,
    LoadoutScope Scope, IReadOnlyDictionary<string, string> SourceHashes);
public sealed record TowerBossReferenceEntry(string ReferenceSetId, int Floor, TowerSearchBudget Budget,
    IReadOnlyList<int> MutablePartySlots, string FixedPartyFingerprint, string AnchorId, string AnchorLabel,
    IReadOnlyList<PartyChoice> Controls, IReadOnlyList<TowerBossReferenceEvidence> Evidence,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PackageId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>>? Contexts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContextsHash = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContextsSourcePath = null);
public sealed record TowerBossReferenceCatalog(int SchemaVersion, TowerBossReferenceProvenance Provenance,
    IReadOnlyList<int> ExcludedCombatSeeds, IReadOnlyList<TowerBossReferenceEntry> Entries,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerBossReferenceProvenance>? PriorProvenance = null);
public sealed record TowerBossReferenceSet(string ReferenceSetId, int Floor, TowerSearchBudget Budget,
    IReadOnlyList<int> MutablePartySlots, string FixedPartyFingerprint, string AnchorId, string AnchorLabel,
    IReadOnlyList<PartyChoice> Controls, IReadOnlyList<int> ExcludedCombatSeeds,
    IReadOnlyList<TowerBossReferenceEvidence> Evidence, TowerBossReferenceProvenance Provenance,
    bool ContentMatchesCurrent, bool ExecutionMatchesCurrent, bool? SettingsMatchCurrent, string EvidenceStatus,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerBossReferenceProvenance>? PriorProvenance = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>>? Contexts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContextsHash = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContextsSourcePath = null);

/// <summary>Portable historical recipes and observations, never current-content win guarantees.</summary>
public static class TowerBossReferences
{
    public const string FileName = "tower-boss-references.json";
    public const string RefinementFileName = "tower-boss-references-refinement.json";
    public const string PilotPackage = "tower-boss-pilot-20260910";
    public const string RefinementPackage = "tower-boss-refinement-20260910";
    public const string PilotManifest = "889ecbd306aad303e508222a6320e07d26aba57b86abc65ef57f6b12b95aa4f2";
    public const string RefinementManifest = "942dd47aa38a8125a518291e57bad0b291e4fa9e11bb26b7bafbb1c1f18e5b7e";
    private static readonly JsonSerializerOptions Options = new(HarnessJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    // The sealed source was written on Windows. Reconstruct those source bytes on every host.
    private static readonly JsonSerializerOptions RefinementSourceOptions = new(HarnessJson.Options) { NewLine = "\r\n" };

    // A run freezes its selected catalog under FileName. Older archives therefore never consult a newer live fixture.
    public static string CatalogPath(string catalogs) => File.Exists(Path.Combine(catalogs, RefinementFileName))
        ? Path.Combine(catalogs, RefinementFileName) : Path.Combine(catalogs, FileName);

    /// <summary>
    /// Includes fixed character identity seeds, gear selections, budget and every other build field.
    /// Only the mutable ordered Essence list is removed; scenario labels and combat seeds are not identities.
    /// </summary>
    public static string FixedPartyFingerprint(TowerScenario scenario) => HarnessJson.Hash(scenario.Party
        .OrderBy(p => p.PartySlot).Select(p => p with { Build = p.Build with { EssenceIds = [] } }).ToArray());

    public static TowerBossReferenceSet? Load(string root, string catalogs, int floor, int slots, TowerSettings? settings = null,
        ExecutionIdentity? execution = null)
    {
        var path = CatalogPath(catalogs);
        if (!File.Exists(path)) return null;
        var catalog = JsonSerializer.Deserialize<TowerBossReferenceCatalog>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Empty boss reference catalog.");
        Validate(catalog);
        var entry = catalog.Entries.SingleOrDefault(e => e.Floor == floor && e.Budget.EssenceSlots == slots);
        if (entry is null) return null;
        var provenance = entry.PackageId is null || entry.PackageId == catalog.Provenance.PackageId
            ? catalog.Provenance : catalog.PriorProvenance!.Single(p => p.PackageId == entry.PackageId);
        var expectedBudget = TowerPartyProgression.Budget(slots) with { PriorityFloor = floor };
        // Avoid Definition(): it consumes this catalog, and historical evidence must not select the current party.
        var current = TowerPartyProgression.Scenarios(root, catalogs, expectedBudget).Single(s => s.FloorNumber == floor);
        if (entry.Budget != expectedBudget || !entry.MutablePartySlots.SequenceEqual(current.Party.Select(p => p.PartySlot).Order())
            || FixedPartyFingerprint(current) != entry.FixedPartyFingerprint)
            throw new InvalidDataException("Boss references do not match the current fixed target identities, gear, progression or required party slots. Rebuild the reference set explicitly before using these controls.");

        var contentMatches = provenance.Scope.ContentHashes.All(p =>
            File.Exists(Path.Combine(root, "Data", p.Key)) && HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) == p.Value);
        // Reconstruction supplies the archived execution identity so a later tool build cannot rewrite historical status.
        var executionMatches = HarnessJson.Hash(provenance.Scope.Execution) == HarnessJson.Hash(execution ?? ExecutionIdentity.Current());
        // Frozen content snapshots omit appsettings. Unknown settings remain unknown, not an assumed match.
        settings ??= File.Exists(Path.Combine(root, "appsettings.json")) ? TowerBundle.ReadSettings(root) : null;
        bool? settingsMatch = settings is null ? null : HarnessJson.Hash(settings) == HarnessJson.Hash(provenance.Scope.Settings);
        var families = new OfflineContent(root, (settings ?? provenance.Scope.Settings).Threat).Essences.GetAll()
            .ToDictionary(e => e.Id, e => e.SourceMonsterId, StringComparer.Ordinal);
        if (entry.Controls.Any(c => c.Builds.Values.Any(ids => ids.Any(id => !families.ContainsKey(id))
            || ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)))
            throw new InvalidDataException("A retained boss reference is no longer legal in the current Essence pool.");
        var isRefinement = provenance.PackageId == RefinementPackage;
        var status = (isRefinement
                ? "Historical refinement observations (40 samples per cell); earlier pilot observations (20 samples) remain separate. Fixed target identities and gear match. "
                : "Historical pilot observations; fixed target identities and gear match. ")
            + (contentMatches ? "Combat content hashes match. " : "Combat content changed; historical wins are not current evidence. ")
            + (executionMatches ? "Execution hashes match. " : "Execution changed; historical wins require fresh confirmation. ")
            + (settingsMatch is true ? "Settings match." : settingsMatch is false ? "Combat settings changed." : "Current settings are unavailable in this content snapshot.");
        return new(entry.ReferenceSetId, entry.Floor, entry.Budget, entry.MutablePartySlots, entry.FixedPartyFingerprint,
            entry.AnchorId, entry.AnchorLabel, entry.Controls, catalog.ExcludedCombatSeeds, entry.Evidence, provenance,
            contentMatches, executionMatches, settingsMatch, status, isRefinement ? catalog.PriorProvenance : null,
            entry.Contexts, entry.ContextsHash, entry.ContextsSourcePath);
    }

    private static bool Hash(string value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool Sources(IReadOnlyDictionary<string, string>? hashes) => hashes is { Count: > 0 }
        && hashes.All(p => !string.IsNullOrWhiteSpace(p.Key) && !Path.IsPathRooted(p.Key)
            && !p.Key.Split('/', '\\').Contains("..") && Hash(p.Value));

    private static void Validate(TowerBossReferenceCatalog c)
    {
        if (c.SchemaVersion is not (1 or 2) || !Provenance(c.Provenance, c.SchemaVersion == 1 ? PilotPackage : RefinementPackage)
            || (c.SchemaVersion == 1 && c.PriorProvenance is not null)
            || (c.SchemaVersion == 2 && (c.PriorProvenance is not { Count: 1 } || !Provenance(c.PriorProvenance[0], PilotPackage)))
            || c.ExcludedCombatSeeds is not { Count: > 0 and <= 100000 }
            || !c.ExcludedCombatSeeds.SequenceEqual(c.ExcludedCombatSeeds.Distinct().Order())
            || c.Entries is not { Count: 3 } || c.Entries.Any(e => e is null || e.Budget is null)
            || !c.Entries.Select(e => (e.Floor, e.Budget.EssenceSlots)).Order().SequenceEqual(new[] { (7, 5), (8, 4), (13, 7) }))
            throw new InvalidDataException("Invalid portable boss reference provenance, source hashes, exclusions or cohort inventory.");
        var excluded = c.ExcludedCombatSeeds.ToHashSet();
        foreach (var e in c.Entries)
        {
            var refined = c.SchemaVersion == 2 && e.Floor != 8;
            var expectedCount = e.Floor == 7 ? (refined ? 42 : 25) : e.Floor == 8 ? 35 : (refined ? 53 : 36);
            var samples = refined ? 40 : 20;
            if (refined)
            {
                if (e.Contexts is not { Count: 2 } || e.ContextsHash is null || !Hash(e.ContextsHash)
                    || HarnessJson.Hash(e.Contexts) != e.ContextsHash
                    || e.ContextsSourcePath != $"studies/floor-{e.Floor}-slots-{e.Budget.EssenceSlots}/contexts.json"
                    || !c.Provenance.SourceHashes.ContainsKey(e.ContextsSourcePath)
                    || Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(e.Contexts, RefinementSourceOptions)))
                        != c.Provenance.SourceHashes[e.ContextsSourcePath]
                    || e.Contexts.Any(context => string.IsNullOrWhiteSpace(context.Key) || context.Value is not { Count: 15 }
                        || context.Value.Any(s => s is null || s.SchemaVersion != 1 || s.PreparationState != "uncleared-no-contributions"
                            || s.Party is not { Count: > 0 } || s.Party.Any(p => p is null || p.Build is null || p.Build.EssenceIds is null))
                        || !context.Value.Select(s => s.FloorNumber).Order().SequenceEqual(Enumerable.Range(1, 15))))
                    throw new InvalidDataException("Refinement references must retain both complete frozen teammate contexts and their source identity.");
            }
            else if (e.Contexts is not null || e.ContextsHash is not null || e.ContextsSourcePath is not null)
                throw new InvalidDataException("Original pilot references must preserve their original fields.");
            if (!TowerBenchmark.SafeId(e.ReferenceSetId) || e.Budget != (TowerPartyProgression.Budget(e.Budget.EssenceSlots) with { PriorityFloor = e.Floor })
                || (c.SchemaVersion == 1 ? e.PackageId is not null : e.PackageId != (refined ? RefinementPackage : PilotPackage))
                || e.MutablePartySlots is not { Count: > 0 } || !e.MutablePartySlots.SequenceEqual(Enumerable.Range(1, e.Floor == 7 ? 5 : 10))
                || !Hash(e.FixedPartyFingerprint) || string.IsNullOrWhiteSpace(e.AnchorLabel)
                || e.Controls is null || e.Controls.Count != expectedCount
                || e.Controls.Any(p => p is null || p.Builds is null || p.Builds.Values.Any(ids => ids is null)) || e.Controls[0].Source != "control"
                || e.Controls.Select(p => p.Id).Distinct().Count() != expectedCount || !e.Controls.Any(p => p.Id == e.AnchorId)
                || e.Evidence is null || e.Evidence.Count != expectedCount || e.Evidence.Any(r => r is null)
                || !e.Controls.Select(p => p.Id).SequenceEqual(e.Evidence.Select(r => r.Id)))
                throw new InvalidDataException("Boss reference cohort must retain every frozen finalist, exact budget and historical anchor.");
            foreach (var (party, row) in e.Controls.Zip(e.Evidence))
            {
                ValidateEvidence(e, party, row, samples, excluded);
                if (refined ? row.WasDiscoveryPrimary is null : row.WasDiscoveryPrimary is not null || row.PriorObservations is not null)
                    throw new InvalidDataException("Boss reference evidence must preserve its original experiment role.");
                if (row.PriorObservations is null) continue;
                if (row.PriorObservations.Count != 1 || row.PriorObservations[0] is not { PackageId: PilotPackage, Evidence: not null } prior
                    || prior.Evidence.WasDiscoveryPrimary is not null || prior.Evidence.PriorObservations is not null)
                    throw new InvalidDataException("Earlier pilot observations must remain a separate original observation with a disjoint sample schedule.");
                ValidateEvidence(e, party, prior.Evidence, 20, excluded);
                if (prior.Evidence.TargetRecipe.Seeds.Intersect(row.TargetRecipe.Seeds).Any())
                    throw new InvalidDataException("Earlier pilot observations must remain a separate original observation with a disjoint sample schedule.");
            }
            if (refined && (e.Evidence.Count(r => r.PriorObservations is not null) != (e.Floor == 7 ? 25 : 36)
                || e.Evidence.Count(r => r.WasDiscoveryPrimary is true) != 1
                || e.Evidence.Single(r => r.WasDiscoveryPrimary is true).Id != e.AnchorId
                || e.AnchorId != (e.Floor == 7
                    ? "68bcf1597a0cba4906df7986ec66583bfb2b4036963b0ebfa7168a20362e3873"
                    : "7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e")))
                throw new InvalidDataException("Refinement references must retain the pilot observations and the discovery-frozen primary anchor, without confirmation-driven reselection.");
            if (!e.Evidence.Single(r => r.Id == e.AnchorId).StrategyLabels.Contains("focused-progress"))
                throw new InvalidDataException("Historical refinement anchor must be the pilot's frozen focused-progress recipe.");
        }
    }

    private static bool Provenance(TowerBossReferenceProvenance? p, string package) => p is not null && p.PackageId == package
        && p.ManifestSha256 == (package == PilotPackage ? PilotManifest : RefinementManifest)
        && !string.IsNullOrWhiteSpace(p.Interpretation) && p.Scope is not null && p.Scope.Settings?.Threat is not null
        && p.Scope.Execution is not null && Sources(p.Scope.Execution.AssemblyHashes) && Sources(p.Scope.ContentHashes) && Sources(p.SourceHashes);

    private static void ValidateEvidence(TowerBossReferenceEntry e, PartyChoice party, TowerBossReferenceEvidence row,
        int samples, HashSet<int> excluded)
    {
        if (party.Id != HarnessJson.Hash(party.Builds) || string.IsNullOrWhiteSpace(party.Source)
            || !party.Builds.Keys.Order().SequenceEqual(e.MutablePartySlots)
            || party.Builds.Values.Any(ids => ids.Count != e.Budget.EssenceSlots || ids.Distinct().Count() != ids.Count)
            || row.Id != party.Id || string.IsNullOrWhiteSpace(row.Label) || string.IsNullOrWhiteSpace(row.OriginalSource)
            || row.StrategyLabels is null || row.DiscoveryOrigins is not { Count: > 0 }
            || row.DiscoveryOrigins.Any(o => o is null || !TowerBossSearch.Methods.Contains(o.Method) || string.IsNullOrWhiteSpace(o.ProposalSource))
            || row.TargetRecipe is null || row.TargetRecipe.SchemaVersion != 1 || row.TargetRecipe.FloorNumber != e.Floor
            || row.TargetRecipe.PreparationState != "uncleared-no-contributions"
            || row.TargetRecipe.Seeds is null || row.TargetRecipe.Seeds.Count != samples || row.TargetRecipe.Seeds.Distinct().Count() != samples
            || row.TargetRecipe.Seeds.Any(seed => !excluded.Contains(seed))
            || row.TargetRecipe.Party is null || row.TargetRecipe.Party.Count != e.MutablePartySlots.Count
            || row.TargetRecipe.Party.Any(p => p is null || p.Build is null || p.Build.EssenceIds is null)
            || FixedPartyFingerprint(row.TargetRecipe) != e.FixedPartyFingerprint
            || row.TargetRecipe.Party.Any(p => !party.Builds.TryGetValue(p.PartySlot, out var ids) || !p.Build.EssenceIds.SequenceEqual(ids))
            || row.TargetRecipeHash != HarnessJson.Hash(row.TargetRecipe) || !Sources(row.SourceHashes)
            || row.Confirmation is null || row.Confirmation.Id != party.Id || row.Confirmation.Cells is not { Count: 30 })
            throw new InvalidDataException("Boss reference recipe, historical observation or fixed party fingerprint is inconsistent.");
        var cells = row.Confirmation.Cells;
        if (cells.Any(cell => cell is null || cell.Clears is null || cell.Trials is null)
            || cells.Select(cell => (cell.Floor, cell.Context)).Distinct().Count() != 30
            || !cells.Select(cell => cell.Floor).Distinct().Order().SequenceEqual(Enumerable.Range(1, 15))
            || cells.GroupBy(cell => cell.Context).Count() != 2
            || cells.Any(cell => cell.Clears.Count != samples || cell.Trials.Count != samples || cell.Trials.Distinct().Count() != samples
                || cell.Draws < 0 || cell.Draws > samples - cell.Clears.Count(x => x)
                || !double.IsFinite(cell.GuardianHealth) || !double.IsFinite(cell.Survival)))
            throw new InvalidDataException("Boss reference confirmation evidence requires its complete original 15-floor matrix.");
        var target = cells.Where(cell => cell.Floor == e.Floor).ToArray();
        if (!target[0].Clears.SequenceEqual(target[1].Clears) || !target[0].Trials.SequenceEqual(target[1].Trials))
            throw new InvalidDataException("Target context aliases in the historical pilot must describe identical observations.");
        if (e.Contexts is not null && (!e.Contexts.Keys.Order().SequenceEqual(cells.Select(cell => cell.Context).Distinct().Order())
            || e.Contexts.Values.Any(scenarios => HarnessJson.Hash(TowerPartySelection.Apply(
                scenarios.Single(s => s.FloorNumber == e.Floor), party.Builds, row.TargetRecipe.Seeds)) != row.TargetRecipeHash)))
            throw new InvalidDataException("Historical target recipes must preserve the exact frozen context, identities, gear and ordered Essences.");
    }
}
