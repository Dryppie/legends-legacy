using Application.Interfaces.Services.LL.Balance;
using Services.LL.Balance;

namespace EssenceSystem.Tests;

public sealed class CombatCalibrationFrameworkTests
{
    private readonly CombatDifficultyEvaluator _evaluator = new();

    [Fact]
    public void Every_encounter_type_and_strength_band_has_an_explicit_envelope()
    {
        foreach (var encounterType in Enum.GetValues<CalibrationEncounterType>())
        {
            foreach (var strength in Enum.GetValues<CalibrationStrengthBand>())
            {
                var envelope = _evaluator.GetEnvelope(encounterType, strength);

                Assert.Equal(encounterType, envelope.EncounterType);
                Assert.Equal(strength, envelope.Strength);
                Assert.True(envelope.WinRatePercent.Minimum <= envelope.WinRatePercent.Maximum);
                Assert.True(envelope.MedianDurationSeconds.Minimum <= envelope.MedianDurationSeconds.Maximum);
                Assert.True(envelope.MedianHealthLostPercent.Minimum <= envelope.MedianHealthLostPercent.Maximum);
            }
        }
    }

    [Fact]
    public void Strength_bands_move_through_real_progression_rungs_in_order()
    {
        var indexes = Enum.GetValues<CalibrationStrengthBand>()
            .Select(strength => CalibrationStrengthBandPolicy.ResolveRungIndex(
                expectedRungIndex: 7,
                maximumAvailableRungIndex: 11,
                strength))
            .ToArray();

        Assert.Equal([6, 7, 8, 9], indexes);
        Assert.Equal(indexes, Enum.GetValues<CalibrationStrengthBand>()
            .Select(strength => CalibrationStrengthBandPolicy.ResolveRungIndex(7, 11, strength)));
    }

    [Fact]
    public void Strength_bands_respect_the_progression_available_at_the_checkpoint()
    {
        Assert.Equal(
            [0, 0, 1, 2],
            Enum.GetValues<CalibrationStrengthBand>()
                .Select(strength => CalibrationStrengthBandPolicy.ResolveRungIndex(0, 2, strength)));
        Assert.Equal(
            [9, 10, 10, 10],
            Enum.GetValues<CalibrationStrengthBand>()
                .Select(strength => CalibrationStrengthBandPolicy.ResolveRungIndex(10, 10, strength)));
    }

    [Fact]
    public void Evaluator_accepts_metrics_inside_every_dimension()
    {
        var assessment = _evaluator.Evaluate(
            CalibrationEncounterType.NormalEnemy,
            CalibrationStrengthBand.Expected,
            Metrics(winRate: 90, duration: 9, healthLost: 25));

        Assert.Equal(CalibrationStatus.WithinTarget, assessment.Status);
        Assert.True(assessment.IsWithinTarget);
        Assert.Null(assessment.EstimatedEnemyHealthAdjustmentPercent);
        Assert.Null(assessment.EstimatedEnemyOffenseAdjustmentPercent);
    }

    [Fact]
    public void Evaluator_distinguishes_durability_from_offensive_pressure()
    {
        var assessment = _evaluator.Evaluate(
            CalibrationEncounterType.NormalEnemy,
            CalibrationStrengthBand.Expected,
            Metrics(winRate: 60, duration: 24, healthLost: 78));

        Assert.Equal(CalibrationStatus.TooHard, assessment.Status);
        Assert.Contains(assessment.Diagnostics, diagnostic =>
            diagnostic.Contains("Defensive durability", StringComparison.Ordinal));
        Assert.Contains(assessment.Diagnostics, diagnostic =>
            diagnostic.Contains("Offensive pressure", StringComparison.Ordinal));
        Assert.True(assessment.EstimatedEnemyHealthAdjustmentPercent < 0);
        Assert.True(assessment.EstimatedEnemyOffenseAdjustmentPercent < 0);
    }

    [Fact]
    public void Conflicting_difficulty_signals_are_reported_as_mixed()
    {
        var assessment = _evaluator.Evaluate(
            CalibrationEncounterType.AreaBoss,
            CalibrationStrengthBand.Expected,
            Metrics(winRate: 95, duration: 180, healthLost: 20));

        Assert.Equal(CalibrationStatus.Mixed, assessment.Status);
    }

    [Fact]
    public void Evaluation_is_repeatable_for_the_same_simulation_metrics()
    {
        var metrics = Metrics(winRate: 87.5, duration: 10.2, healthLost: 31.4);

        var first = _evaluator.Evaluate(
            CalibrationEncounterType.NormalEnemy,
            CalibrationStrengthBand.Expected,
            metrics);
        var second = _evaluator.Evaluate(
            CalibrationEncounterType.NormalEnemy,
            CalibrationStrengthBand.Expected,
            metrics);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Target, second.Target);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
        Assert.Equal(
            first.EstimatedEnemyHealthAdjustmentPercent,
            second.EstimatedEnemyHealthAdjustmentPercent);
        Assert.Equal(
            first.EstimatedEnemyOffenseAdjustmentPercent,
            second.EstimatedEnemyOffenseAdjustmentPercent);
    }

    private static CombatCalibrationMetrics Metrics(
        double winRate,
        double duration,
        double healthLost) =>
        new(
            Samples: 100,
            WinRatePercent: winRate,
            DeathChancePercent: 100 - winRate,
            AverageDurationSeconds: duration,
            MedianDurationSeconds: duration,
            P95DurationSeconds: duration * 1.2,
            AverageHealthLostPercent: healthLost,
            MedianHealthLostPercent: healthLost,
            P95HealthLostPercent: Math.Min(100, healthLost * 1.2),
            AverageRemainingHealthPercent: 100 - healthLost,
            KillsPerMinute: winRate / 100 * 60 / duration,
            AverageDamageTaken: healthLost,
            AverageHealingDone: 0,
            AverageHealthRegenerated: 0);
}
