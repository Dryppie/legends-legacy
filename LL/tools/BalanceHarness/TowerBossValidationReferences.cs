using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerBossValidationObservation(string Trial, int Seed, string Outcome, bool Win,
    string TerminationReason, double DurationSeconds, double GuardianHealthPercent, double PartySurvivalPercent,
    double FriendlyReportedHealing, double FriendlyEffectiveRegeneration, double GuardianReportedHealing,
    double GuardianEffectiveRegeneration);
public sealed record TowerBossValidationEvidence(string Id, string Role, string Label, TowerScenario TargetRecipe,
    string TargetRecipeHash, int Wins, int Defeats, int Draws, IReadOnlyList<TowerBossValidationObservation> Outcomes,
    IReadOnlyDictionary<string, string> SourceHashes);
public sealed record TowerBossValidationCatalog(int SchemaVersion, string Id, int Floor, TowerSearchBudget Budget,
    IReadOnlyList<int> MutablePartySlots, string FixedPartyFingerprint, IReadOnlyList<int> Seeds,
    IReadOnlyList<int> ExcludedCombatSeeds, IReadOnlyList<TowerBossValidationEvidence> Evidence,
    TowerBossReferenceProvenance Provenance);
public sealed record TowerBossValidationSet(TowerBossValidationCatalog Catalog, bool ContentMatchesCurrent,
    bool ExecutionMatchesCurrent, bool? SettingsMatchCurrent, string EvidenceStatus);
public sealed record TowerBossValidationRecovery(double FriendlyReportedHealing, double FriendlyEffectiveRegeneration,
    double GuardianReportedHealing, double GuardianEffectiveRegeneration);
public sealed record TowerBossValidationSummary(int Samples, int Wins, int Defeats, int Draws, RateEstimate Wilson95,
    double MeanGuardianHealthPercent, double MeanPartySurvivalPercent, double? MeanVictoryDurationSeconds,
    double? MeanNonVictoryDurationSeconds, TowerBossValidationRecovery MeanRecovery);
public sealed record TowerBossValidationPair(string CandidateId, string ReferenceId, int Samples,
    int Gained, int Lost, int BothWin, int BothDoNotWin);

/// <summary>Separate fixed validation observations; never a search, anchor replacement or transfer matrix.</summary>
public static class TowerBossValidationReferences
{
    public const string FileName = "tower-boss-validation-references.json";
    public const string PackageId = "nhalia-validation-20260911";
    public const string ManifestSha256 = "d94ad2683d159101987cf5f6e5338a58620fbf929ad34f38ca759be7bc4d103f";
    // The importer checks the sealed manifest and all 486 consumed source files. Pin the resulting portable bytes
    // so observations cannot be rewritten by updating their own counts, hashes or provenance fields.
    public const string FixtureSha256 = "c4cdda4166b23b4c14e6ee9acbfde1200dc0b5453b07bd91e02aba12608c5971";
    private static readonly JsonSerializerOptions Options = new(HarnessJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private static readonly (string Role, string Id)[] Recipes = [
        ("anchor", "a4eaec8b4a9ab78088b9a6e5a672bfdf0a00bab29264d2a3694b2b1f22aa2a18"),
        ("previous-primary", "7f48c238712abd89e1af15b37b53e94d27255712dfc0316ba0f955ec9393744e"),
        ("previous-exploratory", "813457e85da488606bcf34f12b02ec63c67e3966d9aaeb9bcc727239cf2103d1")
    ];

    public static TowerBossValidationCatalog? Read(string catalogs)
    {
        var path = Path.Combine(catalogs, FileName);
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != FixtureSha256)
            throw new InvalidDataException("Portable fixed validation differs from the pinned sealed import.");
        var catalog = JsonSerializer.Deserialize<TowerBossValidationCatalog>(bytes, Options)
            ?? throw new InvalidDataException("Empty fixed validation catalog.");
        Validate(catalog);
        return catalog;
    }

    public static TowerBossValidationSet? Load(string root, string catalogs, TowerSettings? settings = null,
        ExecutionIdentity? execution = null)
    {
        var catalog = Read(catalogs);
        if (catalog is null) return null;
        var current = TowerPartyProgression.Scenarios(root, catalogs, catalog.Budget).Single(s => s.FloorNumber == catalog.Floor);
        if (TowerBossReferences.FixedPartyFingerprint(current) != catalog.FixedPartyFingerprint
            || !current.Party.Select(p => p.PartySlot).Order().SequenceEqual(catalog.MutablePartySlots))
            throw new InvalidDataException("Fixed validation no longer matches current target identities, gear or progression.");
        var provenance = catalog.Provenance;
        var contentMatches = provenance.Scope.ContentHashes.All(pair => File.Exists(Path.Combine(root, "Data", pair.Key))
            && HarnessJson.FileHash(Path.Combine(root, "Data", pair.Key)) == pair.Value);
        var executionMatches = HarnessJson.Hash(provenance.Scope.Execution) == HarnessJson.Hash(execution ?? ExecutionIdentity.Current());
        settings ??= File.Exists(Path.Combine(root, "appsettings.json")) ? TowerBundle.ReadSettings(root) : null;
        bool? settingsMatch = settings is null ? null : HarnessJson.Hash(settings) == HarnessJson.Hash(provenance.Scope.Settings);
        var families = new OfflineContent(root, (settings ?? provenance.Scope.Settings).Threat).Essences.GetAll()
            .ToDictionary(e => e.Id, e => e.SourceMonsterId, StringComparer.Ordinal);
        if (catalog.Evidence.Any(row => row.TargetRecipe.Party.Any(member => member.Build.EssenceIds.Any(id => !families.ContainsKey(id))
            || member.Build.EssenceIds.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalog.Budget.EssenceSlots)))
            throw new InvalidDataException("A fixed validation recipe is no longer legal in the current Essence pool.");
        var status = "Historical fixed validation: 100 paired samples per recipe on floor 13 only. Earlier 20/40-sample all-floor observations and search anchors remain separate and unchanged. "
            + (contentMatches ? "Combat content hashes match. " : "Combat content changed; historical wins are not current evidence. ")
            + (executionMatches ? "Execution hashes match. " : "Execution changed; historical wins require fresh confirmation. ")
            + (settingsMatch is true ? "Settings match." : settingsMatch is false ? "Combat settings changed." : "Current settings are unavailable in this content snapshot.");
        return new(catalog, contentMatches, executionMatches, settingsMatch, status);
    }

    public static TowerBossValidationSummary Summary(TowerBossValidationEvidence evidence)
    {
        var outcomes = evidence.Outcomes;
        double? Duration(bool victory) => outcomes.Where(row => row.Win == victory).Select(row => (double?)row.DurationSeconds).Average();
        return new(outcomes.Count, evidence.Wins, evidence.Defeats, evidence.Draws,
            SuiteScorecard.Wilson(evidence.Wins, outcomes.Count)!, outcomes.Average(row => row.GuardianHealthPercent),
            outcomes.Average(row => row.PartySurvivalPercent), Duration(true), Duration(false),
            new(outcomes.Average(row => row.FriendlyReportedHealing), outcomes.Average(row => row.FriendlyEffectiveRegeneration),
                outcomes.Average(row => row.GuardianReportedHealing), outcomes.Average(row => row.GuardianEffectiveRegeneration)));
    }

    public static IReadOnlyList<TowerBossValidationPair> PairedComparisons(TowerBossValidationCatalog catalog)
    {
        var pairs = new List<TowerBossValidationPair>();
        for (var candidate = 1; candidate < catalog.Evidence.Count; candidate++)
            for (var reference = 0; reference < candidate; reference++)
            {
                var left = catalog.Evidence[candidate]; var right = catalog.Evidence[reference];
                var rows = left.Outcomes.Zip(right.Outcomes).ToArray();
                if (rows.Any(row => row.First.Seed != row.Second.Seed)) throw new InvalidDataException("Validation pairs require the same ordered seed schedule.");
                pairs.Add(new(left.Id, right.Id, rows.Length, rows.Count(row => row.First.Win && !row.Second.Win),
                    rows.Count(row => !row.First.Win && row.Second.Win), rows.Count(row => row.First.Win && row.Second.Win),
                    rows.Count(row => !row.First.Win && !row.Second.Win)));
            }
        return pairs;
    }

    private static bool Hash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool Sources(IReadOnlyDictionary<string, string>? sources) => sources is { Count: > 0 }
        && sources.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && !Path.IsPathRooted(pair.Key)
            && !pair.Key.Split('/', '\\').Contains("..") && Hash(pair.Value));
    private static bool Nonnegative(double value) => double.IsFinite(value) && value >= 0;

    private static void Validate(TowerBossValidationCatalog catalog)
    {
        var provenance = catalog.Provenance;
        if (catalog.SchemaVersion != 1 || catalog.Id != "nhalia-fixed-validation-20260911" || catalog.Floor != 13
            || catalog.Budget != (TowerPartyProgression.Budget(7) with { PriorityFloor = 13 })
            || catalog.MutablePartySlots is null || !catalog.MutablePartySlots.SequenceEqual(Enumerable.Range(1, 10))
            || !Hash(catalog.FixedPartyFingerprint) || catalog.Seeds is not { Count: 100 } || catalog.Seeds.Distinct().Count() != 100
            || catalog.ExcludedCombatSeeds is not { Count: 18687 }
            || !catalog.ExcludedCombatSeeds.SequenceEqual(catalog.ExcludedCombatSeeds.Distinct().Order())
            || catalog.Seeds.Except(catalog.ExcludedCombatSeeds).Any()
            || catalog.Evidence is not { Count: 3 } || catalog.Evidence.Any(row => row is null)
            || !catalog.Evidence.Select(row => (row.Role, row.Id)).SequenceEqual(Recipes)
            || provenance is null || provenance.PackageId != PackageId || provenance.ManifestSha256 != ManifestSha256
            || string.IsNullOrWhiteSpace(provenance.Interpretation) || provenance.Scope?.Settings?.Threat is null
            || provenance.Scope.Execution is null || !Sources(provenance.Scope.Execution.AssemblyHashes)
            || !Sources(provenance.Scope.ContentHashes) || !Sources(provenance.SourceHashes))
            throw new InvalidDataException("Invalid fixed validation provenance, scope, cohort, budget or exclusions.");
        foreach (var row in catalog.Evidence)
        {
            var recipe = row.TargetRecipe;
            if (string.IsNullOrWhiteSpace(row.Label) || recipe is null || recipe.SchemaVersion != 1 || recipe.FloorNumber != 13
                || recipe.PreparationState != "uncleared-no-contributions" || recipe.Seeds is null || !recipe.Seeds.SequenceEqual(catalog.Seeds)
                || recipe.Party is not { Count: 10 } || recipe.Party.Any(member => member is null || member.Build is null || member.Build.EssenceIds is not { Count: 7 })
                || !recipe.Party.Select(member => member.PartySlot).SequenceEqual(catalog.MutablePartySlots)
                || row.Id != HarnessJson.Hash(recipe.Party.ToDictionary(member => member.PartySlot, member => member.Build.EssenceIds))
                || TowerBossReferences.FixedPartyFingerprint(recipe) != catalog.FixedPartyFingerprint
                || row.TargetRecipeHash != HarnessJson.Hash(recipe) || !Sources(row.SourceHashes)
                || row.SourceHashes.Any(pair => !provenance.SourceHashes.TryGetValue(pair.Key, out var hash) || hash != pair.Value)
                || row.Outcomes is not { Count: 100 } || row.Outcomes.Any(outcome => outcome is null)
                || !row.Outcomes.Select(outcome => outcome.Seed).SequenceEqual(catalog.Seeds)
                || row.Outcomes.Select(outcome => outcome.Trial).Distinct().Count() != 100
                || row.Outcomes.Any(outcome => string.IsNullOrWhiteSpace(outcome.Trial) || string.IsNullOrWhiteSpace(outcome.TerminationReason)
                    || outcome.Outcome is not ("Victory" or "Defeat" or "Draw") || outcome.Win != (outcome.Outcome == "Victory")
                    || !Nonnegative(outcome.DurationSeconds) || !Nonnegative(outcome.GuardianHealthPercent) || outcome.GuardianHealthPercent > 100
                    || !Nonnegative(outcome.PartySurvivalPercent) || outcome.PartySurvivalPercent > 100
                    || !Nonnegative(outcome.FriendlyReportedHealing) || !Nonnegative(outcome.FriendlyEffectiveRegeneration)
                    || !Nonnegative(outcome.GuardianReportedHealing) || !Nonnegative(outcome.GuardianEffectiveRegeneration))
                || row.Wins != row.Outcomes.Count(outcome => outcome.Win)
                || row.Defeats != row.Outcomes.Count(outcome => outcome.Outcome == "Defeat")
                || row.Draws != row.Outcomes.Count(outcome => outcome.Outcome == "Draw")
                || row.Wins + row.Defeats + row.Draws != 100)
                throw new InvalidDataException("Fixed validation must preserve every exact recipe, paired outcome and recovery observation.");
        }
    }
}
