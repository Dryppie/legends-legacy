using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

internal sealed record BossGroupDiversityEntry(int CatalogueIndex, BossJoinedGroup Group);

/// <summary>Cover authored structural evidence before revisiting count/filler variants.</summary>
public static class TowerGroupDiversitySearch
{
    public const string Version = "independent-group-diversity-v1";
    public const string Method = "group-diversity-joint";

    internal static IReadOnlyList<BossGroupDiversityEntry> Order(BossJoinedCatalogue catalogue, int seed)
    {
        if (catalogue.Groups.Count > TowerJoinedMechanics.MaximumGroups)
            throw new InvalidDataException("Unbounded group diversity catalogue.");
        using var timing = TowerPerformanceTrace.Measure("generation.group-diversity.order");
        static string[] Canonical(IEnumerable<string> values) => values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var remaining = catalogue.Groups.OrderBy(g => g.Id, StringComparer.Ordinal).Select((g, i) =>
            new BossGroupDiversityEntry(i, g with { EssenceIds = Canonical(g.EssenceIds), SourceCoreIds = Canonical(g.SourceCoreIds), EvidenceKeys = Canonical(g.EvidenceKeys) })).ToList();
        var evidence = new HashSet<string>(StringComparer.Ordinal);
        var cores = new HashSet<string>(StringComparer.Ordinal);
        var essences = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BossGroupDiversityEntry>();
        var label = seed.ToString(CultureInfo.InvariantCulture);
        var ties = remaining.ToDictionary(e => e.Group.Id, e => (uint)StableRandom.Seed(Version, label, e.Group.Id), StringComparer.Ordinal);
        while (remaining.Count > 0)
        {
            var next = remaining.OrderByDescending(e => e.Group.EvidenceKeys.Count(k => !evidence.Contains(k)))
                .ThenByDescending(e => e.Group.SourceCoreIds.Count(k => !cores.Contains(k)))
                .ThenByDescending(e => e.Group.EssenceIds.Count(k => !essences.Contains(k)))
                .ThenBy(e => ties[e.Group.Id]).ThenBy(e => e.Group.Id, StringComparer.Ordinal).First();
            result.Add(next); remaining.Remove(next);
            evidence.UnionWith(next.Group.EvidenceKeys); cores.UnionWith(next.Group.SourceCoreIds); essences.UnionWith(next.Group.EssenceIds);
        }
        return result;
    }

    internal static BossGroupCountChoice Select(IReadOnlyList<BossGroupDiversityEntry> order, int freshIndex, int owners, int seed)
    {
        if (freshIndex < 0 || owners < 1) throw new InvalidDataException("Invalid group diversity schedule position.");
        if (freshIndex % 8 == 7 || order.Count == 0)
            return new(freshIndex, null, order.Count == 0 ? "empty-catalogue-uniform" : "uniform", null, 0, null);
        var guided = freshIndex - freshIndex / 8;
        var anchors = new[] { 1 + (owners - 1) / 2, 1, owners }.Distinct().ToArray();
        var width = anchors.Length + 1;
        var sweep = guided / order.Count;
        var variant = sweep % width;
        var cycle = sweep / width;
        var position = guided % order.Count;
        var entry = order[position];
        var countIndex = variant < anchors.Length ? variant : 0;
        var fillerIndex = variant < anchors.Length ? 0 : 1;
        var count = 1 + (int)(((long)anchors[countIndex] - 1 + cycle) % owners);
        var label = seed.ToString(CultureInfo.InvariantCulture);
        var cycleLabel = cycle.ToString(CultureInfo.InvariantCulture);
        var placement = StableRandom.Seed(Version, "placement", label, entry.Group.Id, cycleLabel);
        var filler = StableRandom.Seed(Version, "filler", label, entry.Group.Id, cycleLabel, fillerIndex.ToString(CultureInfo.InvariantCulture));
        return new(freshIndex, guided, "group-diversity", entry.CatalogueIndex, count, entry.Group,
            new(cycle * order.Count + position, variant, countIndex, fillerIndex, placement, filler));
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private int? diversitySeed;
    private IReadOnlyList<BossGroupDiversityEntry>? diversityOrder;

    internal BossGeneratedChoice FreshGroupDiversity(Random random, int freshIndex, int generationSeed)
    {
        if (input.Generation.PolicyVersion != TowerGroupDiversitySearch.Version || joinedCatalogue is null)
            throw new InvalidDataException("Group diversity construction requires its explicit policy.");
        // The baseline generator is shared by sequential arms. Keep only the active label's bounded order.
        if (diversityOrder is null || diversitySeed != generationSeed)
        {
            diversityOrder = TowerGroupDiversitySearch.Order(joinedCatalogue, generationSeed);
            diversitySeed = generationSeed;
        }
        var schedule = TowerGroupDiversitySearch.Select(diversityOrder, freshIndex, input.RequiredPartySize, generationSeed);
        return schedule.Variation is { } variation
            ? CompleteGroupCount(schedule, new Random(variation.PlacementSeed), new Random(variation.FillerSeed))
            : CompleteGroupCount(schedule, random, random);
    }
}
