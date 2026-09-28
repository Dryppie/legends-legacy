using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Analytics;

public sealed record ItemizationDistribution(string Attribute, int Count, double P10, double P50, double P90, double P99);
public sealed record ItemizationCohort(string Context, int RulesVersion, string Slot, int Tier, string Doctrine,
    int EssenceCount, int Offered, int Equipped, int Dismantled, int Battles, int DistinctCharacters,
    int Wins, int Draws, double? WinRateLower95, double? WinRateUpper95,
    IReadOnlyList<ItemizationDistribution> NormalizedSpend, IReadOnlyList<ItemizationDistribution> CapWaste)
{
    public int Comparisons { get; init; }
    public IReadOnlyList<ItemizationCombination> Combinations { get; init; } = [];
    public IReadOnlyList<ItemizationDistribution> EffectiveAttributes { get; init; } = [];
    public IReadOnlyList<ItemizationEssenceUsage> EssenceUsage { get; init; } = [];
    public IReadOnlyList<ItemizationAttributeOutcome> AttributeOutcomes { get; init; } = [];
}
public sealed record ItemizationCombination(IReadOnlyList<string> Attributes, int Builds, int DistinctCharacters);
public sealed record ItemizationEssenceUsage(string Id, int Ascension, int SlotOrder, int DistinctCharacters, int Battles, int Wins);
public sealed record ItemizationAttributeOutcome(string Attribute, double MinimumInclusive, double? MaximumExclusive,
    int DistinctCharacters, int Battles, double CharacterMeanWinRate, double? Lower95, double? Upper95);
public sealed record ItemizationCohortReport(DateOnly Day, IReadOnlyList<ItemizationCohort> Cohorts)
{
    public IReadOnlyList<ItemizationChoiceSummary> Choices { get; init; } = [];
    public IReadOnlyList<ItemizationOpportunity> SevenDayOpportunities { get; init; } = [];
    public string Interpretation => "Observational cohorts; offers and decisions have different contexts. Repeated battles are clustered by character. Intervals use a conservative Hoeffding bound on independent character averages (at least 30 characters); rates are descriptive, not causal effects.";
    public static ItemizationCohortReport Create(DateOnly day, IEnumerable<ItemizationObservation> observations)
    {
        var cohorts = observations.DistinctBy(x => x.Id).GroupBy(x => new
        {
            x.Context, x.RulesVersion,
            Slot = x.Equipment?.EquipmentType.ToString() ?? "build",
            Tier = x.Equipment?.State.Tier ?? x.Build?.Equipment.Select(e => e.State.Tier).DefaultIfEmpty(0).Max() ?? 0,
            Doctrine = x.Build?.Doctrine?.CombatStyleId ?? "none", EssenceCount = x.Build?.Essences.Count ?? 0
        }).Select(group =>
        {
            var battles = group.Where(x => x.Battle is not null).ToArray();
            // One equal-weight outcome average per character, not a binomial interval over repeated fights.
            var players = battles.GroupBy(x => x.CharacterId).Select(g => g.Average(x => x.Battle!.Outcome == "Win" ? 1d : 0d)).ToArray();
            var mean = players.Length == 0 ? 0 : players.Average();
            var halfWidth = players.Length < 30 ? (double?)null : Math.Sqrt(Math.Log(40d) / (2d * players.Length));
            var spend = group.Where(x => x.Equipment is not null).SelectMany(x => x.Equipment!.Stats.Select(stat =>
                (Name: stat.Key.ToString(), Value: stat.Value * EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(stat.Key, x.Equipment.State.Tier, x.Equipment.StatVersion) / EquipmentTierBudgetCurve.GetScale(x.Equipment.State.Tier))));
            var waste = group.Where(x => x.Build is not null).SelectMany(x => x.Build!.Attributes.Select(a => (Name: a.Attribute.ToString(), Value: a.NormalizedBudgetUnused)));
            return new ItemizationCohort(group.Key.Context, group.Key.RulesVersion, group.Key.Slot, group.Key.Tier,
                group.Key.Doctrine, group.Key.EssenceCount, group.Count(x => x.Kind == "awarded"),
                group.Count(x => x.Kind == "equipped"), group.Count(x => x.Kind == "Dismantle"),
                battles.Length, group.Select(x => x.CharacterId).Distinct().Count(),
                battles.Count(x => x.Battle!.Outcome == "Win"), battles.Count(x => x.Battle!.Outcome == "Draw"),
                halfWidth.HasValue ? Math.Max(0, mean - halfWidth.Value) : null,
                halfWidth.HasValue ? Math.Min(1, mean + halfWidth.Value) : null, Distributions(spend), Distributions(waste))
            {
                Comparisons = group.Count(x => x.Kind == "compared"), Combinations = Combinations(group),
                EffectiveAttributes = Distributions(group.Where(x => x.Build is not null)
                    .DistinctBy(x => (x.CharacterId, x.Build!.Hash)).SelectMany(x => x.Build!.Attributes
                        .Select(a => (Name: a.Attribute.ToString(), Value: a.Effective)))),
                EssenceUsage = battles.Where(x => x.Build is not null).SelectMany(x => x.Build!.Essences.Select(e => (Observation: x, Essence: e)))
                    .GroupBy(x => (x.Essence.Id, x.Essence.Ascension, x.Essence.Order))
                    .Select(g => new ItemizationEssenceUsage(g.Key.Id, g.Key.Ascension, g.Key.Order,
                        g.Select(x => x.Observation.CharacterId).Distinct().Count(), g.Count(),
                        g.Count(x => x.Observation.Battle!.Outcome == "Win"))).OrderBy(x => x.Id).ThenBy(x => x.SlotOrder).ToArray(),
                AttributeOutcomes = AttributeOutcomes(battles)
            };
        }).OrderBy(x => x.Context).ThenBy(x => x.Slot).ThenBy(x => x.Tier).ThenBy(x => x.Doctrine).ToArray();
        return new(day, cohorts) { Choices = ItemizationChoiceSummary.Create(observations) };
    }

    private static IReadOnlyList<ItemizationAttributeOutcome> AttributeOutcomes(IEnumerable<ItemizationObservation> battles)
    {
        double[] boundaries = [0, 10, 20, 40, 60, 100];
        return battles.Where(x => x.Build is not null).SelectMany(x => x.Build!.Attributes.Select(a =>
            (Observation: x, a.Attribute, Band: Array.FindLastIndex(boundaries, boundary => a.Effective >= boundary))))
            .GroupBy(x => (x.Attribute, x.Band)).OrderBy(x => x.Key.Attribute).ThenBy(x => x.Key.Band).Select(group =>
            {
                var players = group.GroupBy(x => x.Observation.CharacterId)
                    .Select(g => g.Average(x => x.Observation.Battle!.Outcome == "Win" ? 1d : 0d)).ToArray();
                var mean = players.Average();
                var width = players.Length < 30 ? (double?)null : Math.Sqrt(Math.Log(40d) / (2d * players.Length));
                var band = Math.Max(0, group.Key.Band);
                return new ItemizationAttributeOutcome(group.Key.Attribute.ToString(), boundaries[band],
                    band + 1 < boundaries.Length ? boundaries[band + 1] : null, players.Length, group.Count(), mean,
                    width.HasValue ? Math.Max(0, mean - width.Value) : null, width.HasValue ? Math.Min(1, mean + width.Value) : null);
            }).ToArray();
    }

    private static IReadOnlyList<ItemizationCombination> Combinations(IEnumerable<ItemizationObservation> observations)
    {
        var occurrences = new List<(string Key, Guid Character, string Build)>();
        foreach (var item in observations.Where(x => x.Build is not null).DistinctBy(x => (x.CharacterId, x.Build!.Hash)))
        {
            var stats = item.Build!.Equipment.SelectMany(x => x.Stats).Where(x => x.Value > 0)
                .Select(x => x.Key.ToString()).Distinct().Order(StringComparer.Ordinal).ToArray();
            for (var i = 0; i < stats.Length; i++)
            for (var j = i + 1; j < stats.Length; j++)
            {
                occurrences.Add(($"{stats[i]}|{stats[j]}", item.CharacterId, item.Build.Hash));
                for (var k = j + 1; k < stats.Length; k++)
                    occurrences.Add(($"{stats[i]}|{stats[j]}|{stats[k]}", item.CharacterId, item.Build.Hash));
            }
        }
        return occurrences.GroupBy(x => x.Key).OrderByDescending(x => x.Count()).ThenBy(x => x.Key)
            .Select(g => new ItemizationCombination(g.Key.Split('|'), g.Count(), g.Select(x => x.Character).Distinct().Count())).ToArray();
    }

    private static ItemizationDistribution[] Distributions(IEnumerable<(string Name, double Value)> values) =>
        values.GroupBy(x => x.Name).OrderBy(x => x.Key).Select(g =>
        {
            var ordered = g.Select(x => x.Value).Order().ToArray();
            double Percentile(double p) => ordered[Math.Clamp((int)Math.Ceiling(p * ordered.Length) - 1, 0, ordered.Length - 1)];
            return new ItemizationDistribution(g.Key, ordered.Length, Percentile(.1), Percentile(.5), Percentile(.9), Percentile(.99));
        }).ToArray();
}
