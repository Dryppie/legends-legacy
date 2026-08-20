namespace Application.Interfaces.Services.LL.Balance;

public enum CombatPacingExecutionLevel
{
    Smoke,
    Development,
    Activation
}

public enum CanonicalCombatRole
{
    Offense,
    Balanced,
    Sustain,
    Defensive
}

public enum CombatPacingScenario
{
    StandardEnemyTtk,
    EliteEnemyTtk,
    SoloBossTtk,
    PartyBossTtk,
    PartyBoss5Ttk,
    PartyBoss10Ttk,
    RawTtd,
    EffectiveTtd,
    OffensiveWindow90,
    OffensiveWindow120,
    OvergearTtk,
    OvergearRawTtd
}

public enum CombatPacingOutcome
{
    Victory,
    Defeat,
    Draw,
    WindowCompleted
}

public sealed record EquipmentCombatPacingRequest(
    CombatPacingExecutionLevel ExecutionLevel = CombatPacingExecutionLevel.Smoke,
    IReadOnlyList<int>? Tiers = null,
    int SeedOffset = 0);

public sealed record CombatPacingSample(
    int Seed,
    int DurationTicks,
    CombatPacingOutcome Outcome,
    double TotalDamage,
    double OpeningBasicAttackPercent,
    double OpeningBurstPercent,
    double RemainingHealthPercent,
    double LateWindowHealthTrendPercentPerSecond = 0,
    double? PressureBreakpoint = null,
    int? ReferenceDurationTicks = null,
    CombatPacingTelemetry? Telemetry = null,
    CooperativeCombatPacingTelemetry? CooperativeTelemetry = null);

public sealed record CooperativeCombatPacingTelemetry(
    int PartySize,
    double GuardianAttentionSharePercent,
    double RestorerAttentionSharePercent,
    double NonGuardianAttentionSharePercent,
    double GuardianThreatGenerated,
    double RestorerThreatGenerated,
    double GuardianIncomingRawDamage,
    double RestorerHealingDone,
    double DamageRedirectedToGuardians,
    int Survivors);

public sealed record CombatPacingTelemetry(
    double BasicAttacks,
    double AbilityActivations,
    double DamageDone,
    double HealingDone,
    double LifeStealHealing,
    double HealthRegenerated,
    double BarrierGenerated,
    double BarrierAbsorbed,
    double IncomingRawDamage,
    double AvoidedDamage,
    double AvoidedAttacks,
    double TypedMitigationPrevented,
    double PhysicalMitigationPrevented,
    double MagicalMitigationPrevented,
    double BlockPrevented,
    double DamageReductionPrevented,
    double FinalHealthDamage);

public sealed record CombatPacingPercentiles(
    int MinimumTicks,
    int P10Ticks,
    int MedianTicks,
    int P90Ticks,
    int MaximumTicks,
    string Method);

public sealed record CombatPacingTargetBand(
    int? MinimumTicks,
    int? MaximumTicks,
    string Description);

public sealed record CombatPacingMeasurement(
    CanonicalCombatRole Role,
    int Tier,
    CombatPacingScenario Scenario,
    int SampleCount,
    CombatPacingPercentiles Durations,
    CombatPacingTargetBand Target,
    int Victories,
    int Defeats,
    int Draws,
    int CompletedWindows,
    double MedianTotalDamage,
    double MaximumOpeningBasicAttackPercent,
    double MaximumOpeningBurstPercent,
    double MedianRemainingHealthPercent,
    double? MedianPressureBreakpoint,
    double? MedianReferenceDurationTicks,
    bool BandPassed,
    bool VolatilityPassed,
    bool ResolutionPassed,
    bool ImmortalityPassed,
    IReadOnlyList<string> Failures,
    CombatPacingTelemetry? MedianTelemetry = null,
    CooperativeCombatPacingTelemetry? MedianCooperativeTelemetry = null)
{
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool Passed =>
        BandPassed && VolatilityPassed && ResolutionPassed && ImmortalityPassed;
}

public sealed record CombatPacingTierStabilityGate(
    CanonicalCombatRole Role,
    CombatPacingScenario Scenario,
    int BaselineTier,
    int Tier,
    double DifferencePercent,
    double TolerancePercent,
    bool PersistentDirectionalDrift,
    bool Passed);

public sealed record CombatPacingOvergearGate(
    CanonicalCombatRole Role,
    int EncounterTier,
    double FasterTtkPercent,
    double LongerRawTtdPercent,
    bool Passed);

public sealed record EquipmentCombatPacingReport(
    int EquipmentBalanceVersion,
    int CombatRulesVersion,
    int ReferenceControlVersion,
    CombatPacingExecutionLevel ExecutionLevel,
    int SeedsPerScenario,
    IReadOnlyList<int> Tiers,
    IReadOnlyList<CombatPacingMeasurement> Measurements,
    IReadOnlyList<CombatPacingTierStabilityGate> TierStability,
    IReadOnlyList<CombatPacingOvergearGate> Overgear,
    bool CanApproveBalanceVersion,
    bool Passed,
    IReadOnlyList<string> Blockers);

public interface ICanonicalCombatPacingSampleSource
{
    Task<CombatPacingSample> RunAsync(
        CanonicalCombatRole role,
        int tier,
        CombatPacingScenario scenario,
        int seed,
        CancellationToken cancellationToken);
}

public interface IEquipmentCombatPacingAnalyzer
{
    Task<EquipmentCombatPacingReport> AnalyzeAsync(
        EquipmentCombatPacingRequest request,
        CancellationToken cancellationToken);
}

public interface IEquipmentCombatPacingArtifactStore
{
    Task WriteAsync(
        EquipmentCombatPacingReport report,
        CancellationToken cancellationToken);
}
