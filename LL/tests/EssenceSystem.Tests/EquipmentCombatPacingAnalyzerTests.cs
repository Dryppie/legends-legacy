using Application.Interfaces.Services.LL.Balance;
using Services.LL.Balance;

namespace EssenceSystem.Tests;

public sealed class EquipmentCombatPacingAnalyzerTests
{
    [Fact]
    public async Task Development_run_enforces_every_gate_and_can_approve_a_passing_profile()
    {
        var artifacts = new CapturingArtifactStore();
        var analyzer = new EquipmentCombatPacingAnalyzer(new PassingSampleSource(), artifacts);

        var report = await analyzer.AnalyzeAsync(
            new EquipmentCombatPacingRequest(
                CombatPacingExecutionLevel.Development,
                Tiers: [1, 2]),
            CancellationToken.None);

        Assert.Equal(250, report.SeedsPerScenario);
        Assert.True(report.Passed);
        Assert.True(report.CanApproveBalanceVersion);
        Assert.Empty(report.Blockers);
        Assert.NotEmpty(report.TierStability);
        Assert.All(report.TierStability, gate => Assert.True(gate.Passed));
        Assert.NotEmpty(report.Overgear);
        Assert.All(report.Overgear, gate => Assert.True(gate.Passed));
        Assert.All(report.Measurements, measurement =>
            Assert.Equal(
                EquipmentCombatPacingAnalyzer.PercentileMethod,
                measurement.Durations.Method));
        Assert.All(report.Measurements, measurement =>
            Assert.Equal(12, measurement.MedianTelemetry!.BasicAttacks));
        Assert.Same(report, artifacts.Report);
    }

    [Fact]
    public async Task Smoke_run_is_structural_evidence_but_cannot_approve_a_balance_version()
    {
        var analyzer = new EquipmentCombatPacingAnalyzer(new PassingSampleSource());

        var report = await analyzer.AnalyzeAsync(
            new EquipmentCombatPacingRequest(Tiers: [1]),
            CancellationToken.None);

        Assert.True(report.Passed);
        Assert.False(report.CanApproveBalanceVersion);
        Assert.Equal(8, report.SeedsPerScenario);
    }

    [Fact]
    public async Task Volatility_timeout_and_immortality_are_machine_readable_blockers()
    {
        var analyzer = new EquipmentCombatPacingAnalyzer(new FailingSampleSource());

        var report = await analyzer.AnalyzeAsync(
            new EquipmentCombatPacingRequest(Tiers: [1]),
            CancellationToken.None);

        Assert.False(report.Passed);
        Assert.False(report.CanApproveBalanceVersion);
        Assert.Contains(report.Blockers, blocker => blocker.Contains("P90 TTK", StringComparison.Ordinal));
        Assert.Contains(report.Blockers, blocker => blocker.Contains("finite pressure breakpoint", StringComparison.Ordinal));
        Assert.Contains(report.Measurements, measurement => !measurement.ResolutionPassed);
        Assert.Contains(report.Measurements, measurement => !measurement.ImmortalityPassed);
    }

    private class PassingSampleSource : ICanonicalCombatPacingSampleSource
    {
        public virtual Task<CombatPacingSample> RunAsync(
            CanonicalCombatRole role,
            int tier,
            CombatPacingScenario scenario,
            int seed,
            CancellationToken cancellationToken)
        {
            var duration = scenario switch
            {
                CombatPacingScenario.StandardEnemyTtk => role switch
                {
                    CanonicalCombatRole.Offense => 110,
                    CanonicalCombatRole.Balanced => 140,
                    CanonicalCombatRole.Sustain => 160,
                    _ => 180
                },
                CombatPacingScenario.EliteEnemyTtk => 520,
                CombatPacingScenario.SoloBossTtk => 1_800,
                CombatPacingScenario.PartyBossTtk or
                    CombatPacingScenario.PartyBoss5Ttk or
                    CombatPacingScenario.PartyBoss10Ttk => 2_100,
                CombatPacingScenario.RawTtd => role switch
                {
                    CanonicalCombatRole.Offense => 360,
                    CanonicalCombatRole.Balanced => 520,
                    CanonicalCombatRole.Sustain => 390,
                    _ => 720
                },
                CombatPacingScenario.EffectiveTtd => role switch
                {
                    CanonicalCombatRole.Offense => 430,
                    CanonicalCombatRole.Balanced => 650,
                    CanonicalCombatRole.Sustain => 1_020,
                    _ => 1_080
                },
                CombatPacingScenario.OffensiveWindow90 => 900,
                CombatPacingScenario.OffensiveWindow120 => 1_200,
                CombatPacingScenario.OvergearTtk => 88,
                CombatPacingScenario.OvergearRawTtd => 115,
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            var outcome = scenario switch
            {
                CombatPacingScenario.RawTtd or CombatPacingScenario.EffectiveTtd or
                    CombatPacingScenario.OvergearRawTtd => CombatPacingOutcome.Defeat,
                CombatPacingScenario.OffensiveWindow90 or CombatPacingScenario.OffensiveWindow120 =>
                    CombatPacingOutcome.WindowCompleted,
                _ => CombatPacingOutcome.Victory
            };
            var totalDamage = scenario switch
            {
                CombatPacingScenario.OffensiveWindow90 => 9_000,
                CombatPacingScenario.OffensiveWindow120 => 12_000,
                _ => 1_000
            };
            int? referenceDuration = scenario is CombatPacingScenario.OvergearTtk or
                CombatPacingScenario.OvergearRawTtd
                ? 100
                : null;
            var cooperativeTelemetry = scenario switch
            {
                CombatPacingScenario.PartyBoss5Ttk => CooperativeTelemetry(5),
                CombatPacingScenario.PartyBoss10Ttk => CooperativeTelemetry(10),
                _ => null
            };
            return Task.FromResult(new CombatPacingSample(
                seed,
                duration,
                outcome,
                totalDamage,
                OpeningBasicAttackPercent: 10,
                OpeningBurstPercent: 30,
                RemainingHealthPercent: 20,
                PressureBreakpoint: 1.25,
                ReferenceDurationTicks: referenceDuration,
                Telemetry: new CombatPacingTelemetry(
                    12, 4, totalDamage, 100, 10, 20, 30, 25,
                    800, 40, 1, 200, 120, 80, 15, 10, 400),
                CooperativeTelemetry: cooperativeTelemetry));
        }

        private static CooperativeCombatPacingTelemetry CooperativeTelemetry(int partySize) => new(
            partySize,
            GuardianAttentionSharePercent: 80,
            RestorerAttentionSharePercent: 5,
            NonGuardianAttentionSharePercent: 20,
            GuardianThreatGenerated: 1_000,
            RestorerThreatGenerated: 250,
            GuardianIncomingRawDamage: 5_000,
            RestorerHealingDone: 3_000,
            DamageRedirectedToGuardians: 500,
            Survivors: partySize);
    }

    private sealed class FailingSampleSource : PassingSampleSource
    {
        public override async Task<CombatPacingSample> RunAsync(
            CanonicalCombatRole role,
            int tier,
            CombatPacingScenario scenario,
            int seed,
            CancellationToken cancellationToken)
        {
            var sample = await base.RunAsync(role, tier, scenario, seed, cancellationToken);
            if (scenario == CombatPacingScenario.StandardEnemyTtk &&
                role == CanonicalCombatRole.Balanced)
            {
                return sample with { DurationTicks = seed % 8 == 0 ? 300 : 120 };
            }
            if (scenario == CombatPacingScenario.EffectiveTtd &&
                role == CanonicalCombatRole.Sustain)
            {
                return sample with
                {
                    DurationTicks = 1_800,
                    Outcome = CombatPacingOutcome.Draw,
                    PressureBreakpoint = null
                };
            }
            return sample;
        }
    }

    private sealed class CapturingArtifactStore : IEquipmentCombatPacingArtifactStore
    {
        public EquipmentCombatPacingReport? Report { get; private set; }

        public Task WriteAsync(
            EquipmentCombatPacingReport report,
            CancellationToken cancellationToken)
        {
            Report = report;
            return Task.CompletedTask;
        }
    }
}
