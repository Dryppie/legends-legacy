namespace BalanceHarness;

/// <summary>Expose authored core combinations under the existing finite team budget; no strength claim.</summary>
public static class TowerCorePortfolioSearch
{
    public const string Version = "independent-team-core-portfolio-v1";
    public const string Method = "team-core-portfolio";
    public const string Operator = "fresh-team-core-portfolio";

    internal static (string Id, string[] Essences)[] SnapshotCores(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossMechanicCore> cores, int essenceSlots)
    {
        if (cores.Count > TowerJointStructuralSearch.MaximumCores
            || cores.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) || c.EssenceIds is null
                || c.EssenceIds.Count < 1 || c.EssenceIds.Count > essenceSlots
                || c.EssenceIds.Any(id => id is null || !families.ContainsKey(id))
                || c.EssenceIds.Distinct(StringComparer.Ordinal).Count() != c.EssenceIds.Count
                || c.EssenceIds.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != c.EssenceIds.Count)
            || cores.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != cores.Count)
            throw new InvalidDataException("Core portfolio requires bounded, unique, family-compatible authored cores.");
        return cores.OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => (c.Id, c.EssenceIds.Order(StringComparer.Ordinal).ToArray())).ToArray();
    }
}
