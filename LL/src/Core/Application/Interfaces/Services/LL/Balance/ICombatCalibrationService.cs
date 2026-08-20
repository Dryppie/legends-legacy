namespace Application.Interfaces.Services.LL.Balance;

public enum CalibrationStrengthBand
{
    Undergeared,
    Expected,
    WellGeared,
    Optimized
}

public enum CalibrationArchetype
{
    Balanced,
    Offensive,
    Defensive,
    Sustain,
    AreaDamage
}

public enum CalibrationEncounterType
{
    NormalEnemy,
    Elite,
    AreaBoss,
    DungeonBoss,
    TowerBoss,
    WorldBoss,
    RaidBoss
}

public enum CalibrationStatus
{
    WithinTarget,
    TooEasy,
    TooHard,
    Mixed,
    InsufficientData
}

public sealed record ProgressionCheckpoint(
    int RegionNumber,
    string RegionKey,
    int AreaNumber,
    string AreaId,
    string AreaName,
    int GlobalStep,
    int CharacterLevel,
    int EquipmentTier,
    int EssenceSlots,
    string ExpectedBuildId,
    int RecommendedCombatRating,
    int BalanceVersion);

public sealed record CalibrationPlayerProfile(
    ProgressionCheckpoint Checkpoint,
    CalibrationStrengthBand Strength,
    CalibrationArchetype Archetype,
    string BuildId,
    string EquipmentQuality,
    string EquipmentRarity,
    int TemperingSteps,
    int EquippedItemCount,
    int EssenceCount,
    int CombatRating,
    int SingleTargetRating,
    int AreaDamageRating,
    int PhysicalDurabilityRating,
    int MagicalDurabilityRating,
    int SustainRating,
    int MaxHealth,
    double Power,
    double Armor,
    double Resistance,
    double AttackSpeed);

public sealed record CalibrationMetricRange(double Minimum, double Maximum)
{
    public bool Contains(double value) => value >= Minimum && value <= Maximum;
    public double Midpoint => (Minimum + Maximum) / 2d;
}

public sealed record CombatDifficultyEnvelope(
    CalibrationEncounterType EncounterType,
    CalibrationStrengthBand Strength,
    string Intent,
    CalibrationMetricRange WinRatePercent,
    CalibrationMetricRange MedianDurationSeconds,
    CalibrationMetricRange MedianHealthLostPercent);

public sealed record CombatCalibrationMetrics(
    int Samples,
    double WinRatePercent,
    double DeathChancePercent,
    double AverageDurationSeconds,
    double MedianDurationSeconds,
    double P95DurationSeconds,
    double AverageHealthLostPercent,
    double MedianHealthLostPercent,
    double P95HealthLostPercent,
    double AverageRemainingHealthPercent,
    double KillsPerMinute,
    double AverageDamageTaken,
    double AverageHealingDone,
    double AverageHealthRegenerated);

public sealed record CombatCalibrationAssessment(
    CalibrationStatus Status,
    CombatDifficultyEnvelope Target,
    IReadOnlyList<string> Diagnostics,
    double? EstimatedEnemyHealthAdjustmentPercent,
    double? EstimatedEnemyOffenseAdjustmentPercent)
{
    public bool IsWithinTarget => Status == CalibrationStatus.WithinTarget;
}

public sealed record AreaCalibrationRequest(
    string AreaId,
    int SimulationsPerEncounter,
    int RandomSeed,
    IReadOnlyList<CalibrationStrengthBand>? StrengthBands = null,
    IReadOnlyList<CalibrationArchetype>? Archetypes = null,
    CalibrationEncounterType EncounterType = CalibrationEncounterType.NormalEnemy);

public sealed record AreaCalibrationEncounterResult(
    Guid CreatureId,
    string CreatureName,
    string CreatureArchetype,
    string DamageProfile,
    string DefenseProfile,
    CalibrationPlayerProfile Player,
    CombatCalibrationMetrics Metrics,
    CombatCalibrationAssessment Assessment);

public sealed record AreaCalibrationReport(
    ProgressionCheckpoint Checkpoint,
    int SimulationsPerEncounter,
    int RandomSeed,
    IReadOnlyList<AreaCalibrationEncounterResult> Encounters,
    IReadOnlyList<string> Outliers,
    string TextReport);

public sealed record ProgressionCurvePoint(
    ProgressionCheckpoint Checkpoint,
    CalibrationPlayerProfile ExpectedPlayer,
    double EnemyHealthMultiplier,
    double EnemyOffenseMultiplier,
    double EnemyDefenseMultiplier,
    double EnemyResistanceMultiplier,
    double PlayerCombatRatingIncreasePercent,
    double EnemyHealthIncreasePercent,
    double EnemyOffenseIncreasePercent);

public sealed record ProgressionCurveReport(
    string RegionKey,
    CalibrationArchetype Archetype,
    IReadOnlyList<ProgressionCurvePoint> Checkpoints,
    IReadOnlyList<string> Warnings,
    string TextReport);

public interface ICombatDifficultyEvaluator
{
    CombatDifficultyEnvelope GetEnvelope(
        CalibrationEncounterType encounterType,
        CalibrationStrengthBand strength);

    CombatCalibrationAssessment Evaluate(
        CalibrationEncounterType encounterType,
        CalibrationStrengthBand strength,
        CombatCalibrationMetrics metrics);
}

public interface ICombatCalibrationService
{
    Task<ProgressionCheckpoint> GetCheckpointAsync(
        string areaId,
        CancellationToken cancellationToken);

    Task<CalibrationPlayerProfile> CreatePlayerAsync(
        ProgressionCheckpoint checkpoint,
        CalibrationStrengthBand strength,
        CalibrationArchetype archetype,
        CancellationToken cancellationToken);

    Task<AreaCalibrationReport> AnalyzeAreaAsync(
        AreaCalibrationRequest request,
        CancellationToken cancellationToken);

    Task<ProgressionCurveReport> CreateProgressionReportAsync(
        string regionKey,
        CalibrationArchetype archetype,
        CancellationToken cancellationToken);
}
