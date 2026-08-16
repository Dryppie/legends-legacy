using Application.Interfaces.Services.LL.Balance;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Balance;

public sealed class EquipmentCombatPacingAnalyzer : IEquipmentCombatPacingAnalyzer
{
    public const int ReferenceControlVersion = 4;
    public const string PercentileMethod = "nearest-rank-on-full-deterministic-sample";

    private static readonly int[] CheckpointTiers = [1, 5, 10, 20, 50, 100];
    private static readonly CanonicalCombatRole[] Roles = Enum.GetValues<CanonicalCombatRole>();

    private readonly ICanonicalCombatPacingSampleSource _samples;
    private readonly IEquipmentCombatPacingArtifactStore? _artifacts;

    public EquipmentCombatPacingAnalyzer(
        ICanonicalCombatPacingSampleSource samples,
        IEquipmentCombatPacingArtifactStore? artifacts = null)
    {
        _samples = samples;
        _artifacts = artifacts;
    }

    public async Task<EquipmentCombatPacingReport> AnalyzeAsync(
        EquipmentCombatPacingRequest request,
        CancellationToken cancellationToken)
    {
        var tiers = ResolveTiers(request.Tiers);
        var seedCount = request.ExecutionLevel switch
        {
            CombatPacingExecutionLevel.Smoke => 8,
            CombatPacingExecutionLevel.Development => EquipmentCombatPacingTargets.DevelopmentSeedCount,
            CombatPacingExecutionLevel.Activation => EquipmentCombatPacingTargets.ActivationSeedCount,
            _ => throw new ArgumentOutOfRangeException(nameof(request.ExecutionLevel))
        };
        var measurements = new List<CombatPacingMeasurement>();

        foreach (var tier in tiers)
        {
            foreach (var role in Roles)
            {
                measurements.Add(await MeasureAsync(
                    role, tier, CombatPacingScenario.StandardEnemyTtk,
                    seedCount, request.SeedOffset, cancellationToken));
                measurements.Add(await MeasureAsync(
                    role, tier, CombatPacingScenario.RawTtd,
                    seedCount, request.SeedOffset, cancellationToken));
                measurements.Add(await MeasureAsync(
                    role, tier, CombatPacingScenario.EffectiveTtd,
                    seedCount, request.SeedOffset, cancellationToken));
                measurements.Add(await MeasureAsync(
                    role, tier, CombatPacingScenario.OffensiveWindow90,
                    seedCount, request.SeedOffset, cancellationToken));
                measurements.Add(await MeasureAsync(
                    role, tier, CombatPacingScenario.OffensiveWindow120,
                    seedCount, request.SeedOffset, cancellationToken));

                if (tier > EquipmentStatBudgetCatalog.MinimumTier)
                {
                    measurements.Add(await MeasureAsync(
                        role, tier, CombatPacingScenario.OvergearTtk,
                        seedCount, request.SeedOffset, cancellationToken));
                    measurements.Add(await MeasureAsync(
                        role, tier, CombatPacingScenario.OvergearRawTtd,
                        seedCount, request.SeedOffset, cancellationToken));
                }
            }

            foreach (var scenario in new[]
                     {
                         CombatPacingScenario.EliteEnemyTtk,
                         CombatPacingScenario.SoloBossTtk,
                         CombatPacingScenario.PartyBossTtk
                     })
            {
                measurements.Add(await MeasureAsync(
                    CanonicalCombatRole.Balanced,
                    tier,
                    scenario,
                    seedCount,
                    request.SeedOffset,
                    cancellationToken));
            }
        }

        var offensiveConsistency = ApplyOffensiveWindowConsistency(
            measurements,
            request.ExecutionLevel);
        measurements = offensiveConsistency.Measurements;
        var stability = BuildTierStability(measurements, tiers);
        var overgear = BuildOvergear(measurements);
        var blockers = measurements
            .SelectMany(measurement => measurement.Failures.Select(failure =>
                $"{measurement.Role} Tier {measurement.Tier} {measurement.Scenario}: {failure}"))
            .Concat(offensiveConsistency.Failures)
            .Concat(stability.Where(gate => !gate.Passed).Select(gate =>
                $"{gate.Role} Tier {gate.Tier} {gate.Scenario}: tier stability changed " +
                $"{gate.DifferencePercent:F2}% (limit {gate.TolerancePercent:F2}%)."))
            .Concat(overgear.Where(gate => !gate.Passed).Select(gate =>
                $"{gate.Role} Tier {gate.EncounterTier + 1} overgear: " +
                $"TTK {gate.FasterTtkPercent:F2}% faster and raw TTD " +
                $"{gate.LongerRawTtdPercent:F2}% longer."))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var passed = blockers.Length == 0;

        var report = new EquipmentCombatPacingReport(
            EquipmentStatBudgetCatalog.BalanceVersion,
            PowerRatingAlgorithm.CombatRulesVersion,
            ReferenceControlVersion,
            request.ExecutionLevel,
            seedCount,
            tiers,
            measurements,
            stability,
            overgear,
            request.ExecutionLevel is not CombatPacingExecutionLevel.Smoke && passed,
            passed,
            blockers);
        if (request.ExecutionLevel is not CombatPacingExecutionLevel.Smoke && _artifacts is not null)
            await _artifacts.WriteAsync(report, cancellationToken);
        return report;
    }

    private async Task<CombatPacingMeasurement> MeasureAsync(
        CanonicalCombatRole role,
        int tier,
        CombatPacingScenario scenario,
        int seedCount,
        int seedOffset,
        CancellationToken cancellationToken)
    {
        var samples = new List<CombatPacingSample>(seedCount);
        for (var index = 0; index < seedCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Paired seeds make cross-tier stability a measurement of the stat curve,
            // not a comparison between unrelated random combat samples.
            var seed = unchecked(seedOffset + (int)role * 10_007 +
                (int)scenario * 1_009 + index);
            samples.Add(await _samples.RunAsync(role, tier, scenario, seed, cancellationToken));
        }

        var durations = Percentiles(samples.Select(sample => sample.DurationTicks));
        var target = TargetFor(role, scenario);
        var failures = new List<string>();
        var warnings = new List<string>();
        var bandPassed = target.MinimumTicks is null ||
            durations.MedianTicks >= target.MinimumTicks && durations.MedianTicks <= target.MaximumTicks;
        if (!bandPassed)
        {
            failures.Add($"median {durations.MedianTicks} ticks is outside " +
                $"{target.MinimumTicks}-{target.MaximumTicks} ticks.");
        }

        var volatilityPassed = true;
        if (scenario == CombatPacingScenario.StandardEnemyTtk &&
            durations.P90Ticks > durations.MedianTicks * 1.5d)
        {
            volatilityPassed = false;
            failures.Add("P90 TTK exceeds 150% of the median.");
        }
        if (scenario == CombatPacingScenario.RawTtd && role != CanonicalCombatRole.Offense &&
            durations.P10Ticks < durations.MedianTicks * 0.7d)
        {
            volatilityPassed = false;
            failures.Add("P10 raw TTD is below 70% of the median.");
        }

        var expectedOutcome = ExpectedOutcome(scenario);
        var unresolved = samples.Count(sample => sample.Outcome != expectedOutcome);
        var resolutionPassed = unresolved == 0;
        if (!resolutionPassed)
            failures.Add($"{unresolved} of {samples.Count} samples did not reach {expectedOutcome}.");

        var maximumBasic = samples.Max(sample => sample.OpeningBasicAttackPercent);
        var maximumBurst = samples.Max(sample => sample.OpeningBurstPercent);
        if (scenario == CombatPacingScenario.StandardEnemyTtk && maximumBasic >= 100d)
        {
            failures.Add("an ordinary opening basic attack defeated the standard enemy.");
            resolutionPassed = false;
        }
        if (scenario == CombatPacingScenario.StandardEnemyTtk &&
            role == CanonicalCombatRole.Offense && maximumBurst > 60d)
        {
            failures.Add($"opening burst reached {maximumBurst:F2}% (exception ceiling 60%).");
            resolutionPassed = false;
        }
        else if (scenario == CombatPacingScenario.StandardEnemyTtk &&
            role == CanonicalCombatRole.Offense && maximumBurst > 45d)
        {
            warnings.Add(
                $"opening burst reached {maximumBurst:F2}%; this exceeds the normal 45% ceiling " +
                "but remains within the declared exceptional-synergy ceiling of 60%.");
        }

        var immortalityPassed = true;
        if (scenario == CombatPacingScenario.EffectiveTtd &&
            role is CanonicalCombatRole.Sustain or CanonicalCombatRole.Defensive)
        {
            var nonFinite = samples.Count(sample =>
                sample.Outcome != CombatPacingOutcome.Defeat && sample.PressureBreakpoint is null);
            if (nonFinite > 0)
            {
                immortalityPassed = false;
                failures.Add($"{nonFinite} samples established no finite pressure breakpoint.");
            }
        }

        return new CombatPacingMeasurement(
            role,
            tier,
            scenario,
            samples.Count,
            durations,
            target,
            samples.Count(sample => sample.Outcome == CombatPacingOutcome.Victory),
            samples.Count(sample => sample.Outcome == CombatPacingOutcome.Defeat),
            samples.Count(sample => sample.Outcome == CombatPacingOutcome.Draw),
            samples.Count(sample => sample.Outcome == CombatPacingOutcome.WindowCompleted),
            Median(samples.Select(sample => sample.TotalDamage)),
            maximumBasic,
            maximumBurst,
            Median(samples.Select(sample => sample.RemainingHealthPercent)),
            NullableMedian(samples.Select(sample => sample.PressureBreakpoint)),
            scenario is CombatPacingScenario.OvergearTtk or CombatPacingScenario.OvergearRawTtd
                ? Median(samples.Select(sample => (double)(sample.ReferenceDurationTicks ?? 0)))
                : null,
            bandPassed,
            volatilityPassed,
            resolutionPassed,
            immortalityPassed,
            failures,
            MedianTelemetry(samples))
        {
            Warnings = warnings
        };
    }

    private static CombatPacingTelemetry? MedianTelemetry(
        IReadOnlyList<CombatPacingSample> samples)
    {
        var values = samples
            .Select(sample => sample.Telemetry)
            .Where(telemetry => telemetry is not null)
            .Cast<CombatPacingTelemetry>()
            .ToArray();
        if (values.Length == 0)
            return null;

        return new CombatPacingTelemetry(
            Median(values.Select(value => value.BasicAttacks)),
            Median(values.Select(value => value.AbilityActivations)),
            Median(values.Select(value => value.DamageDone)),
            Median(values.Select(value => value.HealingDone)),
            Median(values.Select(value => value.LifeStealHealing)),
            Median(values.Select(value => value.HealthRegenerated)),
            Median(values.Select(value => value.BarrierGenerated)),
            Median(values.Select(value => value.BarrierAbsorbed)),
            Median(values.Select(value => value.IncomingRawDamage)),
            Median(values.Select(value => value.AvoidedDamage)),
            Median(values.Select(value => value.AvoidedAttacks)),
            Median(values.Select(value => value.TypedMitigationPrevented)),
            Median(values.Select(value => value.PhysicalMitigationPrevented)),
            Median(values.Select(value => value.MagicalMitigationPrevented)),
            Median(values.Select(value => value.BlockPrevented)),
            Median(values.Select(value => value.DamageReductionPrevented)),
            Median(values.Select(value => value.FinalHealthDamage)));
    }

    private static (List<CombatPacingMeasurement> Measurements, IReadOnlyList<string> Failures)
        ApplyOffensiveWindowConsistency(
            List<CombatPacingMeasurement> measurements,
            CombatPacingExecutionLevel executionLevel)
    {
        var tolerancePercent = executionLevel == CombatPacingExecutionLevel.Smoke
            ? 15d
            : 10d;
        var failures = new List<string>();
        foreach (var shortWindow in measurements.Where(measurement =>
                     measurement.Scenario == CombatPacingScenario.OffensiveWindow90).ToArray())
        {
            var longWindow = measurements.Single(measurement =>
                measurement.Role == shortWindow.Role &&
                measurement.Tier == shortWindow.Tier &&
                measurement.Scenario == CombatPacingScenario.OffensiveWindow120);
            var shortDps = shortWindow.MedianTotalDamage / 90d;
            var longDps = longWindow.MedianTotalDamage / 120d;
            var difference = RelativeDifferencePercent(shortDps, longDps);
            if (difference <= tolerancePercent)
                continue;

            var failure = $"{shortWindow.Role} Tier {shortWindow.Tier}: 90/120-second DPS " +
                $"differs by {difference:F2}% (limit {tolerancePercent:F2}%).";
            failures.Add(failure);
            var index = measurements.IndexOf(shortWindow);
            measurements[index] = shortWindow with
            {
                ResolutionPassed = false,
                Failures = [.. shortWindow.Failures, failure]
            };
        }

        return (measurements, failures);
    }

    private static IReadOnlyList<CombatPacingTierStabilityGate> BuildTierStability(
        IReadOnlyList<CombatPacingMeasurement> measurements,
        IReadOnlyList<int> tiers)
    {
        if (tiers.Count < 2)
            return [];

        var gates = new List<CombatPacingTierStabilityGate>();
        foreach (var role in Roles)
        {
            foreach (var (scenario, tolerance) in new[]
                     {
                         (CombatPacingScenario.StandardEnemyTtk, EquipmentCombatPacingTargets.TierTtkTolerancePercent),
                         (CombatPacingScenario.OffensiveWindow90, EquipmentCombatPacingTargets.TierDamageTolerancePercent),
                         (CombatPacingScenario.RawTtd, EquipmentCombatPacingTargets.TierRawTtdTolerancePercent),
                         (CombatPacingScenario.EffectiveTtd, EquipmentCombatPacingTargets.TierEffectiveTtdTolerancePercent)
                     })
            {
                var series = measurements
                    .Where(measurement => measurement.Role == role && measurement.Scenario == scenario)
                    .OrderBy(measurement => measurement.Tier)
                    .ToArray();
                if (series.Length < 2)
                    continue;
                var baseline = StabilityValue(series[0]);
                var differences = series
                    .Select(measurement => PercentChange(baseline, StabilityValue(measurement)))
                    .ToArray();
                for (var index = 1; index < series.Length; index++)
                {
                    var persistentDrift = index >= 3 &&
                        SameNonZeroDirection(
                            differences
                                .Skip(index - 3)
                                .Take(4)
                                .Zip(
                                    differences.Skip(index - 2).Take(3),
                                    (previous, current) => current - previous));
                    gates.Add(new CombatPacingTierStabilityGate(
                        role,
                        scenario,
                        series[0].Tier,
                        series[index].Tier,
                        differences[index],
                        tolerance,
                        persistentDrift,
                        Math.Abs(differences[index]) <= tolerance && !persistentDrift));
                }
            }
        }

        return gates;
    }

    private static IReadOnlyList<CombatPacingOvergearGate> BuildOvergear(
        IReadOnlyList<CombatPacingMeasurement> measurements)
    {
        var gates = new List<CombatPacingOvergearGate>();
        foreach (var ttk in measurements.Where(measurement =>
                     measurement.Scenario == CombatPacingScenario.OvergearTtk))
        {
            var ttd = measurements.Single(measurement =>
                measurement.Role == ttk.Role && measurement.Tier == ttk.Tier &&
                measurement.Scenario == CombatPacingScenario.OvergearRawTtd);
            var fasterTtk = ComparisonPercent(ttk, invert: true);
            var longerTtd = ComparisonPercent(ttd, invert: false);
            gates.Add(new CombatPacingOvergearGate(
                ttk.Role,
                ttk.Tier - 1,
                fasterTtk,
                longerTtd,
                fasterTtk is >= 6d and <= 18d && longerTtd is >= 8d and <= 18d));
        }
        return gates;
    }

    private static double ComparisonPercent(CombatPacingMeasurement measurement, bool invert)
    {
        var reference = measurement.MedianReferenceDurationTicks ?? 0;
        if (reference <= 0)
            return double.PositiveInfinity;
        return invert
            ? (reference - measurement.Durations.MedianTicks) / reference * 100d
            : (measurement.Durations.MedianTicks - reference) / reference * 100d;
    }

    private static double StabilityValue(CombatPacingMeasurement measurement) =>
        measurement.Scenario == CombatPacingScenario.OffensiveWindow90
            ? measurement.MedianTotalDamage
            : measurement.Durations.MedianTicks;

    private static CombatPacingOutcome ExpectedOutcome(CombatPacingScenario scenario) => scenario switch
    {
        CombatPacingScenario.RawTtd or CombatPacingScenario.EffectiveTtd or
            CombatPacingScenario.OvergearRawTtd => CombatPacingOutcome.Defeat,
        CombatPacingScenario.OffensiveWindow90 or CombatPacingScenario.OffensiveWindow120 =>
            CombatPacingOutcome.WindowCompleted,
        _ => CombatPacingOutcome.Victory
    };

    private static CombatPacingTargetBand TargetFor(
        CanonicalCombatRole role,
        CombatPacingScenario scenario)
    {
        var equipmentRole = (EquipmentCombatRole)(int)role;
        var band = scenario switch
        {
            CombatPacingScenario.StandardEnemyTtk =>
                (EquipmentCombatPacingTargets.GetStandardEnemyTtk(equipmentRole), "standard-enemy TTK"),
            CombatPacingScenario.EliteEnemyTtk =>
                (EquipmentCombatPacingTargets.EliteEnemyTtk, "elite TTK"),
            CombatPacingScenario.SoloBossTtk =>
                (EquipmentCombatPacingTargets.SoloBossTtk, "solo-boss TTK"),
            CombatPacingScenario.PartyBossTtk =>
                (EquipmentCombatPacingTargets.PartyBossTtk, "party-boss TTK"),
            CombatPacingScenario.RawTtd =>
                (EquipmentCombatPacingTargets.GetRawTtd(equipmentRole), "raw TTD"),
            CombatPacingScenario.EffectiveTtd =>
                (EquipmentCombatPacingTargets.GetEffectiveTtd(equipmentRole), "effective TTD"),
            _ => ((CombatDurationBand?)null, "comparison or fixed-window scenario")
        };
        return band.Item1 is { } duration
            ? new CombatPacingTargetBand(duration.MinimumTicks, duration.MaximumTicks, band.Item2)
            : new CombatPacingTargetBand(null, null, band.Item2);
    }

    private static IReadOnlyList<int> ResolveTiers(IReadOnlyList<int>? requested)
    {
        var tiers = requested is null or { Count: 0 }
            ? CheckpointTiers
            : requested.Distinct().Order().ToArray();
        if (tiers.Any(tier => tier is < 1 or > 100))
            throw new ArgumentOutOfRangeException(nameof(requested), "Analyzer tiers must be 1-100.");
        return tiers;
    }

    private static CombatPacingPercentiles Percentiles(IEnumerable<int> values)
    {
        var ordered = values.Order().ToArray();
        return new CombatPacingPercentiles(
            ordered[0],
            NearestRank(ordered, 0.10d),
            NearestRank(ordered, 0.50d),
            NearestRank(ordered, 0.90d),
            ordered[^1],
            PercentileMethod);
    }

    private static int NearestRank(IReadOnlyList<int> ordered, double percentile) =>
        ordered[Math.Clamp((int)Math.Ceiling(percentile * ordered.Count) - 1, 0, ordered.Count - 1)];

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2d
            : ordered[middle];
    }

    private static double? NullableMedian(IEnumerable<double?> values)
    {
        var present = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return present.Length == 0 ? null : Median(present);
    }

    private static double RelativeDifferencePercent(double first, double second)
    {
        var denominator = Math.Max(Math.Abs(first), Math.Abs(second));
        return denominator <= 0 ? 0 : Math.Abs(first - second) / denominator * 100d;
    }

    private static double PercentChange(double baseline, double value) =>
        Math.Abs(baseline) <= 0 ? 0 : (value - baseline) / Math.Abs(baseline) * 100d;

    private static bool SameNonZeroDirection(IEnumerable<double> values)
    {
        var signs = values.Select(Math.Sign).Where(sign => sign != 0).ToArray();
        return signs.Length == 3 && signs.All(sign => sign == signs[0]);
    }
}
