namespace BalanceHarness;

/// <summary>Independent composition/placement search with one outcome-independent Essence order.</summary>
public static class TowerCompositionSearch
{
    public const string Version = "independent-composition-only-v1";
    public const string Method = "composition-only-joint";

    public static bool IsCompositionOnly(string policy) => policy is TowerSuppliedCompositionSearch.Version or TowerSuppliedCompositionSearch.ScheduledVersion or TowerSuppliedCompositionSearch.StandaloneVersion or TowerSuppliedCompositionSearch.IncumbentVersion or Version or TowerJoinedMechanics.Version or TowerGroupCountSearch.Version or TowerGroupVariationSearch.Version or TowerGroupDiversitySearch.Version or TowerGroupCompletionSearch.Version or TowerGroupAllocationSearch.Version or TowerJointStructuralSearch.Version or TowerJointStructuralDiversity.Version or TowerTeamCoverageSearch.Version or TowerFillerDiversitySearch.Version or TowerCorePortfolioSearch.Version or TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion;

    // Ordinal ID order is a serialization convention, not an ability-strength ranking.
    // Keep owners distinct: moving a loadout between characters remains a search choice.
    public static IReadOnlyDictionary<int, IReadOnlyList<string>> CanonicalBuilds(
        IReadOnlyDictionary<int, IReadOnlyList<string>> builds) => builds.OrderBy(p => p.Key)
        .ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Order(StringComparer.Ordinal).ToArray());

    public static bool IsCanonical(IEnumerable<string> essences) =>
        essences.SequenceEqual(essences.Order(StringComparer.Ordinal));
}
