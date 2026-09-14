namespace BalanceHarness;

/// <summary>
/// Discovery-only behavior representatives. Deficit is the engine's time-sampled initial-party
/// health deficit (dead initial members contribute fully). Denial is mean total hostile entity
/// action-denied ticks, including summons; it is duration-dependent, not guardian uptime or a reward.
/// Fixed bins are engineering partitions, never reference-derived targets. Ten ticks are one second.
/// </summary>
internal static class TowerBehaviorArchive
{
    internal const int Capacity = 32;
    private static readonly double[] DenialUpperBounds = [0, 30, 100, 300, 1000, 3000, 10000];

    internal static (int Deficit, int Denial) Cell(BossBehavior behavior)
    {
        if (behavior is null || !double.IsFinite(behavior.HealthDeficit) || behavior.HealthDeficit is < 0 or > 1
            || !double.IsFinite(behavior.DeniedTicks) || behavior.DeniedTicks < 0)
            throw new InvalidDataException("Behavior archive requires finite measured deficit [0,1] and nonnegative denial ticks.");
        var denial = Array.FindIndex(DenialUpperBounds, upper => behavior.DeniedTicks <= upper);
        return (Math.Min(3, (int)(behavior.HealthDeficit * 4)), denial < 0 ? 7 : denial);
    }

    internal static string[] Select(IEnumerable<BossDiscoveryMeasurement> measurements)
    {
        var cells = new HashSet<(int, int)>();
        // Ranking first preserves the overall winner, every original tie-break, and deterministic
        // parent order. A different ordered recipe is never treated as combat-equivalent.
        return TowerBossGeneration.Rank(measurements).Where(row => cells.Add(Cell(row.Behavior)))
            .Select(row => row.Id).ToArray();
    }
}
