using System.Text.Json;

namespace BalanceHarness;

/// <summary>Elite-parent preference only. Ordered recipes remain distinct combat candidates.</summary>
internal static class TowerLoadoutDiversity
{
    internal static string Signature(PartyChoice party) => JsonSerializer.Serialize(
        party.Builds.OrderBy(p => p.Key).Select(p => new {
            PartySlot = p.Key, EssenceIds = p.Value.Order(StringComparer.Ordinal).ToArray()
        }).ToArray(), HarnessJson.Options);

    internal static string[] Select(IEnumerable<BossDiscoveryMeasurement> measurements,
        IReadOnlyDictionary<string, BossGeneratedProposal> proposals)
    {
        var ranked = TowerBossGeneration.Rank(measurements).ToArray();
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<string>();
        foreach (var row in ranked)
        {
            if (selected.Count >= TowerBossGeneration.BeamSize) break;
            if (signatures.Add(Signature(proposals[row.Id].Party!))) selected.Add(row.Id);
        }
        // Preserve capacity when fewer loadouts exist; do not discard their ordered variants.
        foreach (var row in ranked)
        {
            if (selected.Count >= TowerBossGeneration.BeamSize) break;
            if (!selected.Contains(row.Id)) selected.Add(row.Id);
        }
        return selected.ToArray();
    }
}
