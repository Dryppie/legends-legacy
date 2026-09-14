using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerAllocationConfirmationSource(TowerBossDiscoveryDefinition Definition,
    TowerFeedbackComparison Comparison, TowerFeedbackShortlist Shortlist, TowerFeedbackSelection Selection,
    IReadOnlyList<TowerSearchSelected> Controls, TowerBalanceDefinition OriginalConfirmation);
public sealed record TowerAllocationConfirmationSeeds(int MasterSeed, IReadOnlyList<int> Historical,
    IReadOnlyList<int> Confirmation);

/// <summary>A new fixed-family study of the audited, stopped v17 allocation experiment.</summary>
public static class TowerAllocationConfirmation
{
    public const string Policy = "tower-allocation-confirmation-v1";
    public const int Recipes = 94;
    public const int Samples = 512;
    public const int MaximumFights = Recipes * Samples;

    public static void ValidateSource(TowerAllocationConfirmationSource source)
    {
        var d = source.Definition; var c = source.Comparison;
        TowerFeedbackBenchmark.Validate(d);
        if (d.Generation.PolicyVersion != TowerSearchAllocation.Version || c.Status != "Ready"
            || c.Family.Count != Recipes || c.Family.Select(f => f.Id).Distinct().Count() != Recipes
            || !c.Family.Select(f => f.Id).SequenceEqual(c.Family.Select(f => f.Id).Order(StringComparer.Ordinal))
            || c.Family.Any(f => f.Id != TowerFeedbackBenchmark.Id(f.Scenario) || f.Scenario.Seeds.Count != 0))
            throw new InvalidDataException("The full canonical 94-recipe stopped allocation family is required.");
        TowerFeedbackBenchmark.ValidateControls(d, source.Controls, c.AnchorId, c.StrongControlId);
        var ids = c.Family.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var control in source.Controls)
        {
            var saved = c.Family.SingleOrDefault(f => f.Id == control.Id);
            if (saved is null || HarnessJson.Hash(saved.Scenario) != HarnessJson.Hash(control.Scenario)
                || !saved.Sources.Contains(new("saved-control", null, null, null, control.Id)))
                throw new InvalidDataException("Every prior control and origin must remain in the family.");
        }
        var matrix = d.Generation.Seeds.SelectMany(seed => TowerSearchAllocation.ComparisonMethods.Select(method => (method, seed))).ToArray();
        if (!c.OriginalArms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || !c.RescreenedArms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || !source.Shortlist.Arms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || c.OriginalArms.Any(a => !ids.Contains(a.Primary) || !ids.Contains(a.Secondary))
            || c.RescreenedArms.Any(a => !ids.Contains(a.Primary) || !ids.Contains(a.Secondary)))
            throw new InvalidDataException("All six original and screened nominations must be retained.");
        Equal(c.OriginalArms, source.Shortlist.OriginalArms, "original nominees");
        Equal(HarnessJson.Hash(d), source.Shortlist.DefinitionHash, "shortlist definition binding");
        Equal(c.RescreenedArms, source.Selection.Arms, "screened nominees");
        Equal(source.Selection.ShortlistHash, HarnessJson.Hash(source.Shortlist), "selection shortlist binding");
        foreach (var a in c.RescreenedArms)
        {
            var arm = source.Shortlist.Arms.Single(s => s.Method == a.Method && s.Seed == a.Seed);
            if (a.Primary == a.Secondary || !arm.Candidates.Any(s => s.Id == a.Primary) || !arm.Candidates.Any(s => s.Id == a.Secondary))
                throw new InvalidDataException("A saved nominee must belong to its unchanged shortlist.");
        }
        Equal(TowerFeedbackBenchmark.ConfirmationDefinition(d, c), source.OriginalConfirmation, "original frozen confirmation");
    }

    public static TowerAllocationConfirmationSeeds Allocate(TowerAllocationConfirmationSource source,
        JsonElement sourceLedger, JsonElement currentHistory, int masterSeed)
    {
        ValidateSource(source);
        var d = source.Definition;
        var historical = TowerSearchBenchmark.History(sourceLedger).Concat(TowerSearchBenchmark.History(currentHistory))
            .Concat(d.ExcludedCombatSeeds).Concat(d.Generation.Seeds).Concat(d.Stages.Schedules.Values.SelectMany(s =>
                s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).Distinct().Order().ToArray();
        if (historical.Length > TowerStudyLimits.HistoricalSeeds - Samples)
            throw new InvalidDataException("History leaves no capacity for all 512 fresh reservations.");
        var used = historical.ToHashSet(); var fresh = new List<int>();
        for (var attempt = 0; fresh.Count < Samples; attempt++)
        {
            if (attempt >= 100000) throw new InvalidDataException("Seed allocation exhausted its fixed limit.");
            var value = StableRandom.Seed(Policy, masterSeed.ToString(CultureInfo.InvariantCulture), "confirmation", attempt.ToString(CultureInfo.InvariantCulture));
            if (used.Add(value)) fresh.Add(value);
        }
        return new(masterSeed, historical, fresh);
    }

    public static TowerBalanceDefinition Definition(TowerAllocationConfirmationSource source,
        TowerAllocationConfirmationSeeds seeds, string executionHash)
    {
        ValidateSource(source);
        if (seeds.Historical.Count == 0 || seeds.Historical.Count > TowerStudyLimits.HistoricalSeeds - Samples
            || !seeds.Historical.SequenceEqual(seeds.Historical.Distinct().Order())
            || seeds.Confirmation.Count != Samples || seeds.Confirmation.Distinct().Count() != Samples
            || seeds.Confirmation.Intersect(seeds.Historical).Any()
            || source.OriginalConfirmation.Cells.SelectMany(c => c.Scenario.Seeds).Intersect(seeds.Confirmation).Any())
            throw new InvalidDataException("The complete history and 512 unique, fresh shared seeds are required.");
        var result = source.OriginalConfirmation with { Id = Policy, ExecutionHash = executionHash,
            ExcludedCombatSeeds = seeds.Historical, MaximumBattles = MaximumFights,
            Cells = source.OriginalConfirmation.Cells.Select(c => c with {
                Scenario = c.Scenario with { Seeds = seeds.Confirmation }, MinimumSamples = Samples }).ToArray() };
        TowerBalanceEvaluator.Validate(result);
        return result;
    }

    public static TowerFeedbackQuality Quality(TowerAllocationConfirmationSource source, TowerAllocationConfirmationSeeds seeds,
        TowerBalanceDefinition definition, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        Equal(Definition(source, seeds, definition.ExecutionHash), definition, "fresh confirmation definition");
        return TowerFeedbackBenchmark.ConfirmationQuality(definition, source.Comparison, TowerSearchAllocation.Isolated, evidence);
    }

    internal static void Equal<T, U>(T expected, U actual, string label)
    {
        if (HarnessJson.Hash(expected) != HarnessJson.Hash(actual)) throw new InvalidDataException("Saved " + label + " differs.");
    }
}
