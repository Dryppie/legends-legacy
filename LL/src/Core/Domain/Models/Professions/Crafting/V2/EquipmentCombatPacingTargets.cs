namespace Domain.Models.Professions.Crafting.V2;

public enum EquipmentCombatRole
{
    Offense,
    Balanced,
    Sustain,
    Defensive
}

public readonly record struct CombatDurationBand(
    int TargetTicks,
    int MinimumTicks,
    int MaximumTicks);

/// <summary>
/// Versioned, equal-tier combat pacing contract for equipment balance analysis.
/// One combat second is ten engine ticks.
/// </summary>
public static class EquipmentCombatPacingTargets
{
    public const int TicksPerSecond = 10;
    public const int OffensiveBenchmarkTicks = 90 * TicksPerSecond;
    public const int SustainBenchmarkTicks = 120 * TicksPerSecond;
    public const int DevelopmentSeedCount = 250;
    public const int ActivationSeedCount = 1_000;

    public const double TierTtkTolerancePercent = 10d;
    // Whole-point persisted flat stats create a small Tier-1 quantization edge.
    public const double TierDamageTolerancePercent = 9d;
    public const double TierRawTtdTolerancePercent = 12d;
    public const double TierEffectiveTtdTolerancePercent = 15d;

    public static CombatDurationBand GetStandardEnemyTtk(EquipmentCombatRole role) =>
        role switch
        {
            EquipmentCombatRole.Offense => Seconds(11, 9, 14),
            EquipmentCombatRole.Balanced => Seconds(14, 12, 16),
            EquipmentCombatRole.Sustain => Seconds(17, 13, 22),
            EquipmentCombatRole.Defensive => Seconds(18, 14, 21),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    public static CombatDurationBand GetRawTtd(EquipmentCombatRole role) =>
        role switch
        {
            EquipmentCombatRole.Offense => Seconds(36, 30, 42),
            EquipmentCombatRole.Balanced => Seconds(52, 45, 60),
            // Cloth sustain gear spends its defensive budget on recovery throughput
            // and cooldown rather than raw health/armor. Its effective TTD is the role gate.
            EquipmentCombatRole.Sustain => Seconds(39, 34, 45),
            EquipmentCombatRole.Defensive => Seconds(72, 60, 85),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    public static CombatDurationBand GetEffectiveTtd(EquipmentCombatRole role) =>
        role switch
        {
            EquipmentCombatRole.Offense => Seconds(43, 36, 52),
            EquipmentCombatRole.Balanced => Seconds(65, 55, 76),
            EquipmentCombatRole.Sustain => Seconds(102, 85, 120),
            EquipmentCombatRole.Defensive => Seconds(120, 90, 135),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    public static CombatDurationBand EliteEnemyTtk { get; } = Seconds(52, 45, 60);
    public static CombatDurationBand SoloBossTtk { get; } = Seconds(180, 150, 210);
    public static CombatDurationBand PartyBossTtk { get; } = Seconds(210, 180, 240);

    private static CombatDurationBand Seconds(int target, int minimum, int maximum) =>
        new(
            target * TicksPerSecond,
            minimum * TicksPerSecond,
            maximum * TicksPerSecond);
}
