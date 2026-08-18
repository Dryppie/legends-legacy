using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Services.LL.Combat;

internal static class IdleCombatTelemetry
{
    private static readonly Meter Meter = new("LegendsLegacy.IdleCombat");

    private static readonly Histogram<double> ResolveDuration =
        Meter.CreateHistogram<double>("idle_combat.resolve.duration", "ms");
    private static readonly Histogram<long> ResolveEncounters =
        Meter.CreateHistogram<long>("idle_combat.resolve.encounters", "encounters");
    private static readonly Histogram<long> ResolveBatches =
        Meter.CreateHistogram<long>("idle_combat.resolve.batches", "batches");
    private static readonly Histogram<double> TemplatePreparationDuration =
        Meter.CreateHistogram<double>("idle_combat.templates.duration", "ms");
    private static readonly Counter<long> HostileTemplateCacheHits =
        Meter.CreateCounter<long>("idle_combat.templates.hostile_cache_hits");
    private static readonly Counter<long> HostileTemplateCacheMisses =
        Meter.CreateCounter<long>("idle_combat.templates.hostile_cache_misses");
    private static readonly Histogram<double> SimulationDuration =
        Meter.CreateHistogram<double>("idle_combat.simulation.duration", "ms");
    private static readonly Histogram<long> SimulationAllocatedBytes =
        Meter.CreateHistogram<long>("idle_combat.simulation.allocated", "By");
    private static readonly Histogram<double> RewardCalculationDuration =
        Meter.CreateHistogram<double>("idle_combat.rewards.calculation.duration", "ms");
    private static readonly Histogram<double> ProgressionApplyDuration =
        Meter.CreateHistogram<double>("idle_combat.rewards.progression_apply.duration", "ms");
    private static readonly Histogram<double> SettlementDuration =
        Meter.CreateHistogram<double>("idle_combat.rewards.settlement.duration", "ms");

    public static long Start() => Stopwatch.GetTimestamp();

    public static void RecordResolve(long startedAt, int encounters, int batches)
    {
        ResolveDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        ResolveEncounters.Record(encounters);
        ResolveBatches.Record(batches);
    }

    public static void RecordTemplatePreparation(long startedAt, bool reusedHostiles)
    {
        TemplatePreparationDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        if (reusedHostiles)
        {
            HostileTemplateCacheHits.Add(1);
        }
        else
        {
            HostileTemplateCacheMisses.Add(1);
        }
    }

    public static void RecordSimulation(long startedAt, long allocatedBefore)
    {
        SimulationDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        SimulationAllocatedBytes.Record(Math.Max(
            0,
            GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore));
    }

    public static void RecordRewardCalculation(long startedAt) =>
        RewardCalculationDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

    public static void RecordProgressionApply(long startedAt) =>
        ProgressionApplyDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

    public static void RecordSettlement(long startedAt) =>
        SettlementDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
}
