using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerPortfolioConfirmationSource(TowerBossDiscoveryDefinition Definition,
    TowerFeedbackComparison Comparison, TowerFeedbackShortlist Shortlist, TowerFeedbackSelection Selection,
    IReadOnlyList<TowerSearchSelected> Controls);
public sealed record TowerPortfolioConfirmationSeeds(int MasterSeed, IReadOnlyList<int> Historical, IReadOnlyList<int> Confirmation);

/// <summary>A separate fixed-family contract; the original v19 capacity stop remains unchanged.</summary>
public static class TowerPortfolioConfirmation
{
    public const string Policy = "tower-portfolio-confirmation-v1";
    public const string Anchor = "team-1abe76ca1891d97a91d484f0a3662048", StrongControl = "team-a7e5de669c4a17287d84060e8ab6359b";
    public const int Recipes = 253, Samples = 512, MaximumFights = Recipes * Samples;

    public static void ValidateSource(TowerPortfolioConfirmationSource source)
    {
        var d = source.Definition; var c = source.Comparison;
        TowerFeedbackBenchmark.Validate(d);
        if (d.Generation.PolicyVersion != TowerSearchPortfolio.Version || c.Status != "CapacityExceeded"
            || c.Family.Count != Recipes || c.Family.Select(f => f.Id).Distinct().Count() != Recipes
            || !c.Family.Select(f => f.Id).SequenceEqual(c.Family.Select(f => f.Id).Order(StringComparer.Ordinal))
            || c.Family.Any(f => f.Id != TowerFeedbackBenchmark.Id(f.Scenario) || f.Scenario.Seeds.Count != 0 || f.Sources.Count == 0))
            throw new InvalidDataException("The complete canonical 253-recipe capacity-stopped v19 family is required.");
        TowerFeedbackBenchmark.ValidateControls(d, source.Controls, c.AnchorId, c.StrongControlId);
        var matrix = d.Generation.Seeds.SelectMany(seed => TowerSearchPortfolio.ComparisonMethods.Select(method => (method, seed))).ToArray();
        if (!c.OriginalArms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || !c.RescreenedArms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || !source.Shortlist.Arms.Select(a => (a.Method, a.Seed)).SequenceEqual(matrix)
            || source.Shortlist.Policy != TowerSearchPortfolio.Policy || source.Shortlist.SchemaVersion != 1)
            throw new InvalidDataException("All six original and screened nomination groups must remain in their frozen order.");
        Equal(c.OriginalArms, source.Shortlist.OriginalArms, "original nominations");
        Equal(HarnessJson.Hash(d), source.Shortlist.DefinitionHash, "shortlist definition");
        Equal(c.RescreenedArms, source.Selection.Arms, "screened nominations");
        Equal(source.Selection.ShortlistHash, HarnessJson.Hash(source.Shortlist), "selection shortlist");
        foreach (var control in source.Controls)
        {
            var saved = c.Family.SingleOrDefault(f => f.Id == control.Id);
            if (saved is null || !saved.Sources.Contains(new("saved-control", null, null, null, control.Id)))
                throw new InvalidDataException("Every prior control and origin must remain in the family.");
            Equal(saved.Scenario, control.Scenario, "control recipe");
        }
        foreach (var arm in c.RescreenedArms)
        {
            var shortlist = source.Shortlist.Arms.Single(a => a.Method == arm.Method && a.Seed == arm.Seed);
            var original = c.OriginalArms.Single(a => a.Method == arm.Method && a.Seed == arm.Seed);
            if (arm.Primary == arm.Secondary || original.Primary == original.Secondary || shortlist.Candidates.Count != TowerFeedbackBenchmark.Width
                || !TowerContractJson.Hash(arm.EvidenceHash)) throw new InvalidDataException("Invalid frozen nomination.");
            foreach (var id in new[] { original.Primary, original.Secondary, arm.Primary, arm.Secondary })
            {
                var recipe = shortlist.Candidates.SingleOrDefault(r => r.Id == id);
                var saved = c.Family.SingleOrDefault(r => r.Id == id);
                if (recipe is null || saved is null) throw new InvalidDataException("Missing original or screened nominee.");
                Equal(recipe.Scenario with { Seeds = [] }, saved.Scenario, "nominee recipe");
            }
        }
        // Reuse the unchanged cohort/role/interval construction without changing the old capacity gate.
        var schedule = d.Stages.Schedules.Values.Single();
        TowerBalanceEvaluator.Validate(Template(source, schedule.Confirmation, SourceHistory(source).Except(schedule.Confirmation).ToArray(), d.ExecutionHash));
    }

    internal static int[] SourceHistory(TowerPortfolioConfirmationSource source) => source.Definition.ExcludedCombatSeeds
        .Concat(source.Definition.Generation.Seeds).Concat(source.Definition.Stages.Schedules.Values.SelectMany(s =>
            s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).Distinct().Order().ToArray();

    public static TowerPortfolioConfirmationSeeds Allocate(TowerPortfolioConfirmationSource source, JsonElement sourceLedger, JsonElement history, int seed)
    {
        ValidateSource(source);
        Equal(SourceHistory(source), TowerSearchBenchmark.History(sourceLedger), "complete v19 reservations");
        var excluded = SourceHistory(source).Concat(TowerSearchBenchmark.History(history)).Distinct().Order().ToArray();
        if (excluded.Length > TowerStudyLimits.HistoricalSeeds - Samples) throw new InvalidDataException("No capacity for the complete fresh schedule.");
        var used = excluded.ToHashSet(); var fresh = new List<int>();
        for (var attempt = 0; fresh.Count < Samples; attempt++)
        {
            if (attempt >= 100000) throw new InvalidDataException("Seed allocation exhausted its fixed limit.");
            var value = StableRandom.Seed(Policy, seed.ToString(CultureInfo.InvariantCulture), "confirmation", attempt.ToString(CultureInfo.InvariantCulture));
            if (used.Add(value)) fresh.Add(value);
        }
        return new(seed, excluded, fresh);
    }

    public static TowerBalanceDefinition Definition(TowerPortfolioConfirmationSource source, TowerPortfolioConfirmationSeeds seeds, string executionHash)
    {
        ValidateSource(source);
        if (seeds.Historical.Count > TowerStudyLimits.HistoricalSeeds - Samples
            || !seeds.Historical.SequenceEqual(seeds.Historical.Distinct().Order())
            || SourceHistory(source).Except(seeds.Historical).Any() || seeds.Confirmation.Count != Samples
            || seeds.Confirmation.Distinct().Count() != Samples || seeds.Confirmation.Intersect(seeds.Historical).Any())
            throw new InvalidDataException("All old reservations and exactly 512 fresh shared values are required.");
        var definition = Template(source, seeds.Confirmation, seeds.Historical, executionHash);
        TowerBalanceEvaluator.Validate(definition); return definition;
    }

    private static TowerBalanceDefinition Template(TowerPortfolioConfirmationSource source, IReadOnlyList<int> seeds,
        IReadOnlyList<int> excluded, string executionHash)
    {
        var d = source.Definition; var context = d.Contexts[0];
        var cohort = new TowerBalanceCohort("fixed-cohort", d.Budget, d.RequiredPartySize, context.Id,
            TowerBossDiscovery.EquipmentBudgetHash(context.CharacterTemplates), d.BudgetPurpose);
        return new(2, Policy, TowerBalanceEvaluator.IntervalPolicy, d.ContentHashes, d.SettingsHash, executionHash, [cohort],
            source.Comparison.Family.Select(f => new TowerBalanceCellDefinition(f.Id, cohort.Id,
                f.Sources.Any(s => s.Method == "saved-control") ? "reference" : "generated", f.Scenario with { Seeds = seeds }, Samples)).ToArray(),
            excluded, MaximumFights);
    }

    public static TowerFeedbackQuality Quality(TowerPortfolioConfirmationSource source, TowerPortfolioConfirmationSeeds seeds,
        TowerBalanceDefinition definition, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        Equal(Definition(source, seeds, definition.ExecutionHash), definition, "fresh confirmation definition");
        return TowerFeedbackBenchmark.ConfirmationQuality(definition, source.Comparison, TowerSearchPortfolio.Portfolio, evidence);
    }

    internal static void Equal<T, U>(T expected, U actual, string label) => TowerAllocationConfirmation.Equal(expected, actual, label);
}
