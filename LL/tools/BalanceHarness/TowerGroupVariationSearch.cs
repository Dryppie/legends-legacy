using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossGroupVariation(int GroupVisit, int VariantIndex, int CountIndex, int FillerIndex,
    int PlacementSeed, int FillerSeed);

/// <summary>Spend existing fresh requests on count and filler variants of one content-derived group.</summary>
public static class TowerGroupVariationSearch
{
    public const string Version = "independent-group-variation-v1";
    public const string Method = "group-variation-joint";

    internal static BossGroupCountChoice Select(BossJoinedCatalogue catalogue, int freshIndex, int owners, int seed)
    {
        if (freshIndex < 0 || owners < 1) throw new InvalidDataException("Invalid group variation schedule position.");
        if (freshIndex % 8 == 7 || catalogue.Groups.Count == 0)
            return new(freshIndex, null, catalogue.Groups.Count == 0 ? "empty-catalogue-uniform" : "uniform", null, 0, null);
        var guided = freshIndex - freshIndex / 8;
        var counts = new[] { 1, 1 + (owners - 1) / 2, owners }.Distinct().ToArray();
        // One baseline filler stream at each count, then a second draw at the middle count.
        // Deduplicate count anchors for one/two-owner fixtures instead of spending repeated requests.
        var width = counts.Length + 1;
        var visit = guided / width;
        var variant = guided % width;
        var countIndex = variant < counts.Length ? variant : counts.Length / 2;
        var fillerIndex = variant < counts.Length ? 0 : 1;
        var groups = catalogue.Groups.OrderBy(g => g.Id, StringComparer.Ordinal).ToArray();
        var label = seed.ToString(CultureInfo.InvariantCulture);
        var offset = (int)((uint)StableRandom.Seed(Version, label) % (uint)groups.Length);
        var index = (visit % groups.Length + offset) % groups.Length;
        var group = groups[index];
        var sweep = visit / groups.Length;
        var count = 1 + (int)(((long)counts[countIndex] - 1 + sweep) % owners);
        var visitLabel = visit.ToString(CultureInfo.InvariantCulture);
        var placementSeed = StableRandom.Seed(Version, "placement", label, group.Id, visitLabel);
        var fillerSeed = StableRandom.Seed(Version, "filler", label, group.Id, visitLabel,
            fillerIndex.ToString(CultureInfo.InvariantCulture));
        return new(freshIndex, guided, "group-variation", index, count, group,
            new(visit, variant, countIndex, fillerIndex, placementSeed, fillerSeed));
    }
}

public sealed partial class TowerBossPartyGenerator
{
    internal BossGeneratedChoice FreshGroupVariation(Random random, int freshIndex, int generationSeed)
    {
        if (input.Generation.PolicyVersion != TowerGroupVariationSearch.Version || joinedCatalogue is null)
            throw new InvalidDataException("Group variation construction requires its explicit policy.");
        var schedule = TowerGroupVariationSearch.Select(joinedCatalogue, freshIndex, input.RequiredPartySize, generationSeed);
        // Placement is nested across counts and identical for the two middle-count filler draws.
        // Filler draw identity is independent of mutable arm RNG consumption and of measured fitness.
        return schedule.Variation is { } variation
            ? CompleteGroupCount(schedule, new Random(variation.PlacementSeed), new Random(variation.FillerSeed))
            : CompleteGroupCount(schedule, random, random);
    }
}
