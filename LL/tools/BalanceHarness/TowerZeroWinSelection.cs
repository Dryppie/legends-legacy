namespace BalanceHarness;

// Callers validate their complete selection matrix before ranking. Discovery rank is frozen
// before selection; confirmation and historical measurements are not inputs to this rule.
internal static class TowerZeroWinSelection
{
    internal static IOrderedEnumerable<T> Rank<T>(IEnumerable<T> rows, Func<T, int> wins,
        Func<T, double> meanGuardianHealth, Func<T, int> discoveryRank, Func<T, string> id) => rows
        .OrderByDescending(wins).ThenBy(row => wins(row) == 0 ? meanGuardianHealth(row) : 0)
        .ThenBy(discoveryRank).ThenBy(id, StringComparer.Ordinal);
}
