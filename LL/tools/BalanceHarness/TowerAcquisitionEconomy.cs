using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items.Equipments.Progression;

namespace BalanceHarness;

public sealed record TowerAcquisitionPace(string Id, double DungeonSuccessProbability,
    double SuccessfulRunMinutes, double FailedRunMinutes, double IdleVictoryProbability,
    double EligibleIdleHoursPerDay, string? DailyProphecyProfile, int DailyProphecyClaims);
public sealed record TowerAcquisitionEffort(int SuccessfulCharacterCompletions, double ExpectedAttempts,
    double ExpectedFailures, IReadOnlyDictionary<string, double> ExpectedEntryItems,
    double AssemblyOnlyFragments, double DungeonActiveHours,
    double RandomSigilOnlyIdleHours, double? FragmentOnlyRewardDays,
    double? CombinedSupplyRateDays, string Interpretation);

/// <summary>Analytic, seed-free conditional expectations. No simulated player telemetry.</summary>
public static class TowerAcquisitionEconomy
{
    public static TowerAcquisitionEffort Calculate(int successes, TowerAcquisitionPace pace,
        DungeonDefinition dungeon, CombatAcquisitionRules drops, DungeonSigilAssemblySettings assembly,
        int cadenceSeconds, int dailyFragments, bool useEveryRegionalSigil)
    {
        if (successes < 0 || cadenceSeconds <= 0 || !assembly.Enabled || assembly.FragmentCost <= 0
            || !double.IsFinite(pace.DungeonSuccessProbability) || pace.DungeonSuccessProbability is <= 0 or > 1
            || !double.IsFinite(pace.IdleVictoryProbability) || pace.IdleVictoryProbability is <= 0 or > 1
            || !double.IsFinite(pace.EligibleIdleHoursPerDay) || pace.EligibleIdleHoursPerDay is < 0 or > 24
            || !double.IsFinite(pace.SuccessfulRunMinutes) || pace.SuccessfulRunMinutes is <= 0 or >= 2880
            || !double.IsFinite(pace.FailedRunMinutes) || pace.FailedRunMinutes is < 0 or > 2880
            || dailyFragments < 0 || pace.DailyProphecyClaims is < 0 or > 1)
            throw new InvalidDataException("Declare finite probabilities, durations, activity and at most one selected daily claim.");
        var costs = dungeon.EntryCosts.GroupBy(c => c.ItemId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));
        if (!costs.TryGetValue(dungeon.SigilItemId, out var sigils) || sigils <= 0
            || costs.Any(c => c.Value <= 0) || drops.Region != dungeon.Region
            || !drops.Sigils.Any(s => s.ItemBaseId == dungeon.SigilItemId))
            throw new InvalidDataException("Dungeon entry costs must include an available regional sigil.");
        // Unsupported additional entry resources must not silently become free time.
        if (costs.Count != 1)
            throw new InvalidDataException("Additional entry resources need an explicit income model before time can be estimated.");
        var attempts = successes / pace.DungeonSuccessProbability;
        var failures = attempts - successes;
        var requiredSigils = attempts * sigils;
        var usableChance = drops.SigilDropChance / (useEveryRegionalSigil ? 1 : drops.Sigils.Count);
        var sigilsPerIdleHour = 3600d / cadenceSeconds * pace.IdleVictoryProbability * usableChance;
        var fragmentRate = dailyFragments * pace.DailyProphecyClaims;
        var combinedRate = sigilsPerIdleHour * pace.EligibleIdleHoursPerDay + (double)fragmentRate / assembly.FragmentCost;
        var expectedEntries = useEveryRegionalSigil
            ? drops.Sigils.ToDictionary(s => s.ItemBaseId, _ => requiredSigils / drops.Sigils.Count)
            : costs.ToDictionary(c => c.Key, c => attempts * c.Value);
        return new(successes, attempts, failures, expectedEntries,
            requiredSigils * assembly.FragmentCost,
            (successes * pace.SuccessfulRunMinutes + failures * pace.FailedRunMinutes) / 60,
            requiredSigils / sigilsPerIdleHour,
            fragmentRate > 0 ? requiredSigils * assembly.FragmentCost / fragmentRate : null,
            combinedRate > 0 ? requiredSigils / combinedRate : null,
            "Conditional on independent stationary success/victory rates, zero starting stock, no trading and supplied durations. "
            + "Random-only hours and attempts are expectations, not guarantees. Reward days and combined days are continuous rate-budget equivalents, "
            + "not expected first-passage times or calendar completion forecasts. No credit for one-time quests, random gear, caches, shops or overlapping activities. "
            + "All-regional entry totals assume the uniform random family mix, with assembled sigils split evenly; run performance is assumed identical across usable families. "
            + "Character effort sums cannot be read as expedition wall time.");
    }
}
