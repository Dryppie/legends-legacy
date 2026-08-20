using Application.Interfaces.Services.LL.Balance;

namespace Services.LL.Balance;

/// <summary>
/// Versioned pacing policy for developer calibration. The simulator reports facts;
/// this class alone decides whether those facts match the intended encounter role.
/// </summary>
public sealed class CombatDifficultyEvaluator : ICombatDifficultyEvaluator
{
    public const int TargetVersion = 1;

    private static readonly IReadOnlyDictionary<
        (CalibrationEncounterType Encounter, CalibrationStrengthBand Strength),
        CombatDifficultyEnvelope> Envelopes = CreateEnvelopes();

    public CombatDifficultyEnvelope GetEnvelope(
        CalibrationEncounterType encounterType,
        CalibrationStrengthBand strength) =>
        Envelopes[(encounterType, strength)];

    public CombatCalibrationAssessment Evaluate(
        CalibrationEncounterType encounterType,
        CalibrationStrengthBand strength,
        CombatCalibrationMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        var target = GetEnvelope(encounterType, strength);
        if (metrics.Samples < 3)
        {
            return new CombatCalibrationAssessment(
                CalibrationStatus.InsufficientData,
                target,
                [$"Only {metrics.Samples} samples were collected; at least 3 are required for calibration."],
                null,
                null);
        }

        var diagnostics = new List<string>();
        var hardSignals = 0;
        var easySignals = 0;

        Compare(
            "Win rate",
            metrics.WinRatePercent,
            target.WinRatePercent,
            lowMeansHard: true,
            "Survival is below the intended envelope.",
            "Survival is above the intended envelope.");
        Compare(
            "Median duration",
            metrics.MedianDurationSeconds,
            target.MedianDurationSeconds,
            lowMeansHard: false,
            "Defensive durability is below the intended envelope.",
            "Defensive durability is above the intended envelope.");
        Compare(
            "Median health lost",
            metrics.MedianHealthLostPercent,
            target.MedianHealthLostPercent,
            lowMeansHard: false,
            "Offensive pressure is below the intended envelope.",
            "Offensive pressure is above the intended envelope.");

        var status = (hardSignals, easySignals) switch
        {
            (0, 0) => CalibrationStatus.WithinTarget,
            (> 0, 0) => CalibrationStatus.TooHard,
            (0, > 0) => CalibrationStatus.TooEasy,
            _ => CalibrationStatus.Mixed
        };

        if (status == CalibrationStatus.WithinTarget)
            diagnostics.Add($"All measured dimensions are within the {target.Intent} envelope.");

        var healthAdjustment = target.MedianDurationSeconds.Contains(metrics.MedianDurationSeconds)
            ? null
            : EstimateAdjustment(target.MedianDurationSeconds.Midpoint, metrics.MedianDurationSeconds);
        var offenseAdjustment = target.MedianHealthLostPercent.Contains(metrics.MedianHealthLostPercent)
            ? null
            : EstimateAdjustment(target.MedianHealthLostPercent.Midpoint, metrics.MedianHealthLostPercent);

        return new CombatCalibrationAssessment(
            status,
            target,
            diagnostics,
            healthAdjustment,
            offenseAdjustment);

        void Compare(
            string metric,
            double value,
            CalibrationMetricRange range,
            bool lowMeansHard,
            string lowDiagnostic,
            string highDiagnostic)
        {
            if (value < range.Minimum)
            {
                if (lowMeansHard)
                    hardSignals++;
                else
                    easySignals++;
                diagnostics.Add(
                    $"{metric} is {value:N1}; target is {range.Minimum:N1}-{range.Maximum:N1}. {lowDiagnostic}");
            }
            else if (value > range.Maximum)
            {
                if (lowMeansHard)
                    easySignals++;
                else
                    hardSignals++;
                diagnostics.Add(
                    $"{metric} is {value:N1}; target is {range.Minimum:N1}-{range.Maximum:N1}. {highDiagnostic}");
            }
        }
    }

    private static double? EstimateAdjustment(double target, double actual)
    {
        if (actual <= 0 || !double.IsFinite(actual))
            return null;

        return Math.Round(Math.Clamp(target / actual - 1d, -0.75d, 2d) * 100d, 1);
    }

    private static IReadOnlyDictionary<
        (CalibrationEncounterType, CalibrationStrengthBand),
        CombatDifficultyEnvelope> CreateEnvelopes()
    {
        var result = new Dictionary<
            (CalibrationEncounterType, CalibrationStrengthBand),
            CombatDifficultyEnvelope>();

        Add(
            CalibrationEncounterType.NormalEnemy,
            "repeatable idle farming",
            undergeared: ((45, 88), (8, 25), (30, 90)),
            expected: ((82, 100), (5, 15), (10, 50)),
            wellGeared: ((95, 100), (2.5, 10), (0, 28)),
            optimized: ((98, 100), (1, 7), (0, 18)));
        Add(
            CalibrationEncounterType.Elite,
            "dangerous optional farming",
            undergeared: ((20, 65), (18, 55), (55, 100)),
            expected: ((70, 95), (12, 40), (30, 80)),
            wellGeared: ((88, 100), (7, 28), (12, 55)),
            optimized: ((96, 100), (4, 20), (0, 35)));
        Add(
            CalibrationEncounterType.AreaBoss,
            "area progression gate",
            undergeared: ((5, 45), (35, 150), (75, 100)),
            expected: ((55, 88), (25, 120), (45, 95)),
            wellGeared: ((82, 100), (16, 85), (20, 70)),
            optimized: ((95, 100), (10, 60), (5, 45)));
        Add(
            CalibrationEncounterType.DungeonBoss,
            "single-party dungeon climax",
            undergeared: ((0, 35), (50, 220), (80, 100)),
            expected: ((50, 85), (40, 180), (50, 98)),
            wellGeared: ((78, 100), (25, 125), (25, 80)),
            optimized: ((93, 100), (15, 90), (8, 55)));
        Add(
            CalibrationEncounterType.TowerBoss,
            "build-sensitive progression check",
            undergeared: ((0, 25), (60, 240), (85, 100)),
            expected: ((40, 75), (45, 200), (60, 100)),
            wellGeared: ((70, 95), (30, 150), (35, 90)),
            optimized: ((88, 100), (20, 110), (15, 70)));
        Add(
            CalibrationEncounterType.WorldBoss,
            "large-group endurance encounter (provisional)",
            undergeared: ((0, 20), (120, 600), (90, 100)),
            expected: ((35, 75), (90, 480), (65, 100)),
            wellGeared: ((65, 95), (60, 360), (40, 95)),
            optimized: ((85, 100), (45, 300), (20, 80)));
        Add(
            CalibrationEncounterType.RaidBoss,
            "coordinated party encounter (provisional)",
            undergeared: ((0, 20), (90, 480), (90, 100)),
            expected: ((45, 80), (75, 360), (65, 100)),
            wellGeared: ((72, 98), (50, 270), (35, 90)),
            optimized: ((90, 100), (35, 210), (15, 70)));

        return result;

        void Add(
            CalibrationEncounterType type,
            string intent,
            ((double Min, double Max) Win, (double Min, double Max) Duration, (double Min, double Max) Health) undergeared,
            ((double Min, double Max) Win, (double Min, double Max) Duration, (double Min, double Max) Health) expected,
            ((double Min, double Max) Win, (double Min, double Max) Duration, (double Min, double Max) Health) wellGeared,
            ((double Min, double Max) Win, (double Min, double Max) Duration, (double Min, double Max) Health) optimized)
        {
            AddBand(CalibrationStrengthBand.Undergeared, undergeared);
            AddBand(CalibrationStrengthBand.Expected, expected);
            AddBand(CalibrationStrengthBand.WellGeared, wellGeared);
            AddBand(CalibrationStrengthBand.Optimized, optimized);

            void AddBand(
                CalibrationStrengthBand strength,
                ((double Min, double Max) Win, (double Min, double Max) Duration, (double Min, double Max) Health) values)
            {
                result[(type, strength)] = new CombatDifficultyEnvelope(
                    type,
                    strength,
                    intent,
                    new CalibrationMetricRange(values.Win.Min, values.Win.Max),
                    new CalibrationMetricRange(values.Duration.Min, values.Duration.Max),
                    new CalibrationMetricRange(values.Health.Min, values.Health.Max));
            }
        }
    }
}
