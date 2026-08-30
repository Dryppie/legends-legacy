using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Stats;

namespace EssenceSystem.Tests;

public sealed class CompactCombatTelemetryTests
{
    [Fact]
    public void Engine_preserves_compact_telemetry_when_full_event_log_capture_is_disabled()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly);
        var hostile = Combatant("hostile", CombatTeam.Hostile);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 2,
                BasicAttackIntervalTicks: 1,
                CaptureEventLog: false));

        var result = engine.Run([friendly], [hostile]);

        Assert.Empty(result.EventLog);
        Assert.Equal(1, result.CompactTelemetry.PeakActiveFriendlyCombatants);
        Assert.Equal(1, result.CompactTelemetry.PeakActiveHostileCombatants);
        Assert.Equal(1, result.CompactTelemetry.TotalFriendlyCombatants);
        Assert.Equal(1, result.CompactTelemetry.TotalHostileCombatants);
        Assert.Contains(result.EntityStats, stats =>
            stats.EntityId == "friendly"
            && stats.TargetInteractions.Any(target => target.TargetId == "hostile" && target.DamageDone > 0));
    }

    [Fact]
    public void Compact_telemetry_can_be_disabled_without_changing_combat_outcome_or_core_stats()
    {
        var enabled = RunEngine(captureCompactTelemetry: true);
        var disabled = RunEngine(captureCompactTelemetry: false);

        Assert.Equal(enabled.Outcome, disabled.Outcome);
        Assert.Equal(enabled.Duration, disabled.Duration);
        Assert.Equal(
            enabled.EntityStats.Single(stats => stats.EntityId == "friendly").DamageDone,
            disabled.EntityStats.Single(stats => stats.EntityId == "friendly").DamageDone);
        Assert.Equal(enabled.Duration, enabled.CompactTelemetry.InitialFriendlyHealthDeficitSampleTicks);
        Assert.InRange(enabled.CompactTelemetry.AverageInitialFriendlyHealthDeficitRatio, 0, 1);
        Assert.True(enabled.CompactTelemetry.AverageInitialFriendlyHealthDeficitRatio > 0);
        Assert.NotEqual(new CompactCombatTelemetry(), enabled.CompactTelemetry);
        Assert.Equal(new CompactCombatTelemetry(), disabled.CompactTelemetry);
        Assert.Empty(disabled.EntityStats.Single(stats => stats.EntityId == "friendly").TargetInteractions);
    }

    [Fact]
    public void Compact_telemetry_samples_initial_friendly_health_deficit_once_per_completed_tick()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly);
        var hostile = Combatant("hostile", CombatTeam.Hostile);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1,
                CaptureEventLog: false));

        var result = engine.Run([friendly], [hostile]);
        var expectedDeficit = 1 - friendly.Health / friendly.GetAttribute(AttributeType.MaxHealth);

        Assert.Equal(1, result.Duration);
        Assert.Equal(1, result.CompactTelemetry.InitialFriendlyHealthDeficitSampleTicks);
        Assert.Equal(expectedDeficit, result.CompactTelemetry.AverageInitialFriendlyHealthDeficitRatio, 6);
    }

    [Fact]
    public void Compact_telemetry_records_the_first_hostile_summon_clear_window()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly);
        var guardian = Combatant("guardian", CombatTeam.Hostile);
        var summon = new RuntimeCombatant(
            "guardian-summon",
            "Guardian Summon",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Power] = 0
            },
            [],
            ["Summoned"],
            isSummoned: true,
            summonDurationTicks: 2,
            summonOwner: guardian);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 5,
                BasicAttackIntervalTicks: 1_000,
                CaptureEventLog: false));

        var result = engine.Run([friendly], [guardian, summon]);

        Assert.Equal(0, result.CompactTelemetry.FirstAdditionalHostileTick);
        Assert.True(result.CompactTelemetry.FirstAdditionalHostileClearTick > 0);
        Assert.Equal(1, result.CompactTelemetry.AdditionalHostileWindowCount);
        Assert.Equal(1, result.CompactTelemetry.ClearedAdditionalHostileWindowCount);
        Assert.Equal(1, result.CompactTelemetry.HostileSummonWaveCount);
        Assert.True(result.CompactTelemetry.HostileSummonActiveTicks > 0);
        Assert.Equal(0, result.CompactTelemetry.FinalActiveHostileSummons);
    }

    [Fact]
    public void Compact_telemetry_leaves_clear_tick_empty_when_hostile_summons_survive()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly);
        var guardian = Combatant("guardian", CombatTeam.Hostile);
        var summon = new RuntimeCombatant(
            "guardian-summon",
            "Guardian Summon",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Power] = 0
            },
            [],
            ["Summoned"],
            isSummoned: true,
            summonDurationTicks: 100,
            summonOwner: guardian);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 2,
                BasicAttackIntervalTicks: 1_000,
                CaptureEventLog: false));

        var result = engine.Run([friendly], [guardian, summon]);

        Assert.Equal(0, result.CompactTelemetry.FirstAdditionalHostileTick);
        Assert.Null(result.CompactTelemetry.FirstAdditionalHostileClearTick);
        Assert.Equal(1, result.CompactTelemetry.AdditionalHostileWindowCount);
        Assert.Equal(0, result.CompactTelemetry.ClearedAdditionalHostileWindowCount);
        Assert.Equal(1, result.CompactTelemetry.FinalActiveHostileSummons);
    }

    [Fact]
    public void Compact_telemetry_counts_distinct_summon_waves_windows_and_uptime()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.telemetry.summon",
            Kind = AbilitySpecKind.Active,
            Name = "Telemetry Summon",
            CooldownTicks = 3,
            Effects =
            [
                new()
                {
                    Id = "effect.telemetry.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "telemetrySummon",
                    DurationTicks = 2
                }
            ]
        };
        var abilities = AbilityCompiler.CompileAbilities([summonAbility]);
        var summons = AbilityCompiler.CompileSummons(
        [
            new SummonSpec
            {
                Id = "telemetrySummon",
                Name = "Telemetry Summon",
                DurationTicks = 2,
                MaxActive = 1,
                Attributes =
                [
                    new() { Attribute = AttributeType.MaxHealth, BaseValue = 1_000, MinimumValue = 1 },
                    new() { Attribute = AttributeType.Power, BaseValue = 0 }
                ]
            }
        ]);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            summons,
            abilities,
            new FastCombatEngineOptions(
                MaxTicks: 6,
                BasicAttackIntervalTicks: 1_000,
                CaptureEventLog: false));

        var result = engine.Run(
            [Combatant("friendly", CombatTeam.Friendly)],
            [Combatant("guardian", CombatTeam.Hostile, abilities.Values)]);

        Assert.Equal(2, result.CompactTelemetry.TotalHostileSummons);
        Assert.Equal(2, result.CompactTelemetry.HostileSummonWaveCount);
        Assert.Equal(1, result.CompactTelemetry.HostileSummonWaveIntervalCount);
        Assert.Equal(3, result.CompactTelemetry.HostileSummonWaveIntervalTotalTicks);
        Assert.Equal(3, result.CompactTelemetry.MinimumHostileSummonWaveIntervalTicks);
        Assert.Equal(3, result.CompactTelemetry.MaximumHostileSummonWaveIntervalTicks);
        Assert.Equal(2, result.CompactTelemetry.AdditionalHostileWindowCount);
        Assert.Equal(2, result.CompactTelemetry.ClearedAdditionalHostileWindowCount);
        Assert.Equal(2, result.CompactTelemetry.HostileSummonActiveTicks);
    }

    [Fact]
    public void Compact_telemetry_stays_within_the_event_log_off_allocation_budget()
    {
        RunEngine(captureCompactTelemetry: false);
        RunEngine(captureCompactTelemetry: true);

        var disabledAllocations = Enumerable.Range(0, 3)
            .Select(_ => MeasureAllocations(captureCompactTelemetry: false))
            .Min();
        var enabledAllocations = Enumerable.Range(0, 3)
            .Select(_ => MeasureAllocations(captureCompactTelemetry: true))
            .Min();

        Assert.True(
            enabledAllocations <= disabledAllocations + 64 * 1024,
            $"Compact telemetry allocated {enabledAllocations - disabledAllocations:N0} additional bytes; budget is 65,536 bytes.");
    }

    [Fact]
    public void Compact_accumulator_preserves_target_lifecycle_and_typed_mechanic_measurements()
    {
        var log = new[]
        {
            Event("player", "guardian", "Strike", EventType.Damage, 50, 10),
            Event("player", "ally", "Mend", EventType.Heal, 20, 11),
            Event("player", "guardian", "condition.stun", EventType.StatusEffect, 1, 12),
            Event("player", "ally", "effect.cleanse", EventType.StatusEffectCleansed, 2, 15),
            Event("guardian", "summon-1", "effect.summon", EventType.Summon, 1, 20),
            Event("player", "summon-1", "Strike", EventType.Death, 0, 30)
        };
        var teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["player"] = "Friendly",
            ["ally"] = "Friendly",
            ["guardian"] = "Hostile",
            ["summon-1"] = "Hostile"
        };

        var stats = new CombatStatsAggregator().Aggregate(log, teams);
        var player = stats.Single(entity => entity.EntityId == "player");
        var guardian = stats.Single(entity => entity.EntityId == "guardian");
        var summon = stats.Single(entity => entity.EntityId == "summon-1");

        Assert.Equal(50, player.TargetInteractions.Single(target => target.TargetId == "guardian").DamageDone);
        Assert.Equal(20, player.TargetInteractions.Single(target => target.TargetId == "ally").HealingDone);
        Assert.Equal(1, player.StunApplications);
        Assert.Equal(2, player.StatusEffectsCleansed);
        Assert.Equal(1, guardian.SummonsCreated);
        Assert.True(summon.IsSummonedEntity);
        Assert.Equal(20, summon.SummonedAtTick);
        Assert.Equal(30, summon.FirstDeathTick);
        Assert.Equal(30, summon.SummonEndedAtTick);
    }

    private static CombatLogItem Event(
        string actor,
        string target,
        string source,
        EventType eventType,
        int magnitude,
        int tick) => new()
    {
        ActorId = actor,
        TargetId = target,
        Source = source,
        StatsSource = source,
        EventType = eventType,
        Magnitude = magnitude,
        Timestamp = tick,
        CombatEntity = new SimpleCombatEntity { Id = target, Name = target }
    };

    private static RuntimeCombatant Combatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility>? abilities = null) => new(
        id,
        id,
        team,
        new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 1_000,
            [AttributeType.Power] = 50,
            [AttributeType.CritDamage] = 100,
            [AttributeType.AttackSpeed] = 0
        },
        abilities ?? [],
        ["Role.Test"]);

    private static CombatResult RunEngine(bool captureCompactTelemetry)
    {
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 512,
                BasicAttackIntervalTicks: 1,
                CaptureEventLog: false,
                CaptureCompactTelemetry: captureCompactTelemetry));
        return engine.Run(
            [Combatant("friendly", CombatTeam.Friendly)],
            [Combatant("hostile", CombatTeam.Hostile)]);
    }

    private static long MeasureAllocations(bool captureCompactTelemetry)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = RunEngine(captureCompactTelemetry);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(result);
        return allocated;
    }
}
