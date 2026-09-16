using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerCompositionOrderCase(string Id, string Composition, string Ordering,
    string SourceId, string SourceRecipeHash, TowerScenario Scenario);
public sealed record TowerCompositionOrderContrast(string Id, string Left, string Right);
public sealed record TowerCompositionOrderPlan(string Version, int Samples, int MaximumFights,
    IReadOnlyList<TowerCompositionOrderCase> Cases, IReadOnlyList<TowerCompositionOrderContrast> Contrasts);
public sealed record TowerCompositionOrderResult(IReadOnlyDictionary<string, RateEstimate> Rates,
    IReadOnlyDictionary<string, TowerSearchBenchmarkPair> Differences, string Scope);

/// <summary>Control-derived sensitivity probes. Never an independent search or a balance acceptance gate.</summary>
public static class TowerCompositionOrderDiagnostic
{
    public const string Version = "tower-composition-order-diagnostic-v1";
    public const int Samples = 64, MaximumFights = 384;
    public static readonly string[] Orderings = ["original", "ascending", "descending"];

    private static TowerScenario Clone(TowerScenario s) => System.Text.Json.JsonSerializer.Deserialize<TowerScenario>(
        System.Text.Json.JsonSerializer.Serialize(s, HarnessJson.Options), HarnessJson.Options)!;

    private static string Context(TowerScenario s) => HarnessJson.Hash(s with
    {
        Id = Version, Seeds = [], Assumptions = [],
        Party = s.Party.Select(p => p with { Build = p.Build with { EssenceIds = [] } }).ToArray()
    });

    public static TowerCompositionOrderPlan Create(string controlId, TowerScenario control,
        string challengerId, TowerScenario challenger)
    {
        if (string.IsNullOrWhiteSpace(controlId) || string.IsNullOrWhiteSpace(challengerId) || controlId == challengerId
            || control.Seeds.Count != 0 || challenger.Seeds.Count != 0
            || control.Party.Count == 0 || control.Party.Count != challenger.Party.Count
            || control.Party.Concat(challenger.Party).Any(p => p.Build.EssenceIds.Count != 5
                || p.Build.EssenceIds.Distinct(StringComparer.Ordinal).Count() != 5
                || p.Build.IdentityEssenceIds is not { Count: 5 })
            || Context(control) != Context(challenger))
            throw new InvalidDataException("Use two seed-free five-Essence recipes with identical materialized context and identities.");
        var cases = new List<TowerCompositionOrderCase>();
        foreach (var (composition, id, source) in new[] { ("control", controlId, control), ("challenger", challengerId, challenger) })
        foreach (var order in Orderings)
        {
            var copy = Clone(source);
            var scenario = copy with { Id = Version, Seeds = [], Assumptions = ["Control-derived composition/order sensitivity diagnostic; no search acceptance."],
                Party = copy.Party.Select(p => p with { Build = p.Build with { EssenceIds = order switch
                {
                    "ascending" => p.Build.EssenceIds.Order(StringComparer.Ordinal).ToArray(),
                    "descending" => p.Build.EssenceIds.OrderDescending(StringComparer.Ordinal).ToArray(),
                    _ => p.Build.EssenceIds.ToArray()
                } } }).ToArray() };
            cases.Add(new(composition + "-" + order, composition, order, id, TowerBossDiscovery.RecipeHash(source.Party), scenario));
        }
        if (cases.Select(c => TowerBossDiscovery.RecipeHash(c.Scenario.Party)).Distinct().Count() != 6)
            throw new InvalidDataException("All six frozen diagnostic recipes must be distinct; do not silently duplicate or trim.");
        return new(Version, Samples, MaximumFights, cases,
        [new("composition-original", "control-original", "challenger-original"),
         new("composition-ascending", "control-ascending", "challenger-ascending"),
         new("composition-descending", "control-descending", "challenger-descending"),
         new("order-control", "control-ascending", "control-descending"),
         new("order-challenger", "challenger-ascending", "challenger-descending")]);
    }

    public static void Validate(TowerCompositionOrderPlan p)
    {
        if (p.Version != Version || p.Samples != Samples || p.MaximumFights != MaximumFights || p.Cases.Count != 6 || p.Contrasts.Count != 5)
            throw new InvalidDataException("Changed diagnostic shape.");
        var control = p.Cases.Single(c => c.Id == "control-original");
        var challenger = p.Cases.Single(c => c.Id == "challenger-original");
        var expected = Create(control.SourceId, control.Scenario, challenger.SourceId, challenger.Scenario);
        // Source hashes describe the same ordered recipe, independent of scenario labels.
        TowerPortfolioConfirmation.Equal(expected, p, "fixed composition/order cases and contrasts");
    }

    public static TowerBalanceDefinition Bind(TowerCompositionOrderPlan plan, TowerBalanceCohort cohort,
        IReadOnlyDictionary<string, string> content, string settingsHash, string executionHash,
        IReadOnlyList<int> historical, IReadOnlyList<int> seeds)
    {
        Validate(plan);
        if (seeds.Count != Samples || seeds.Distinct().Count() != Samples || seeds.Intersect(historical).Any()
            || !historical.SequenceEqual(historical.Distinct().Order()))
            throw new InvalidDataException("Exactly 64 external fresh reservations are required; no reassignment or seed-free execution.");
        var definition = new TowerBalanceDefinition(1, Version, TowerBalanceEvaluator.IntervalPolicy, content, settingsHash, executionHash,
            [cohort with { Id = "diagnostic-cohort" }], plan.Cases.Select(c => new TowerBalanceCellDefinition(c.Id, "diagnostic-cohort",
                "reference", c.Scenario with { Seeds = seeds.ToArray() }, Samples)).ToArray(), historical.ToArray(), MaximumFights);
        TowerBalanceEvaluator.Validate(definition); return definition;
    }

    public static TowerSearchBenchmarkPair Difference(IReadOnlyList<TowerBalanceTrial> left, IReadOnlyList<TowerBalanceTrial> right)
    {
        if (left.Count != Samples || right.Count != Samples || !left.Select(t => t.Seed).SequenceEqual(right.Select(t => t.Seed))
            || left.Select(t => t.Seed).Distinct().Count() != Samples
            || left.Concat(right).Any(t => !Enum.IsDefined(t.Outcome)))
            throw new InvalidDataException("The complete same-order paired schedule is required.");
        var gains = left.Zip(right).Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
        var losses = left.Zip(right).Count(p => p.First.Outcome != BattleOutcome.Victory && p.Second.Outcome == BattleOutcome.Victory);
        // .025 across five contrasts, each using two discordance intervals: .05 / 20 per interval.
        var g = TowerBalanceEvaluator.Wilson(gains, Samples, 20)!;
        var l = TowerBalanceEvaluator.Wilson(losses, Samples, 20)!;
        return new(Samples, gains, losses, (gains-losses)/(double)Samples, g.Lower-l.Upper, g.Upper-l.Lower);
    }

    public static TowerCompositionOrderResult Assess(TowerCompositionOrderPlan plan, TowerBalanceDefinition definition,
        IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        Validate(plan);
        if (definition.MaximumBattles != MaximumFights || definition.Cells.Count != 6
            || !definition.Cells.Select(c => c.Id).SequenceEqual(plan.Cases.Select(c => c.Id))
            || definition.Cells.Any(c => c.Scenario.Seeds.Count != Samples)
            || definition.Cells.Zip(plan.Cases).Any(pair => HarnessJson.Hash(pair.First.Scenario with { Seeds = [] }) != HarnessJson.Hash(pair.Second.Scenario)))
            throw new InvalidDataException("Changed diagnostic assessment matrix.");
        TowerFeedbackBenchmark.RequireEvidence(definition, evidence);
        var rows = evidence.ToDictionary(e => e.CellId);
        // .025 across six rates: existing .05-family helper with twelve seats.
        var rates = rows.ToDictionary(p => p.Key, p => TowerBalanceEvaluator.Wilson(p.Value.Trials.Count(t => t.Outcome == BattleOutcome.Victory), Samples, 12)!);
        var differences = plan.Contrasts.ToDictionary(c => c.Id, c => Difference(rows[c.Left].Trials, rows[c.Right].Trials));
        return new(rates, differences,
            "Exploratory control-derived sensitivity only. Five prespecified left-minus-right paired contrasts; approximate simultaneous Wilson bounds. " +
            "No viability, optimizer adoption, search recovery, interaction significance or ordering-invariance claim. An interval spanning zero is unresolved.");
    }
}
