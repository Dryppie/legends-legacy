using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class FastCombatEngineListenerDispatchTests
{
    [Fact]
    public void Event_without_listener_avoids_event_materialization()
    {
        var delayedListener = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.delayed.hit.listener",
            Kind = AbilitySpecKind.Passive,
            Name = "Delayed Hit Listener",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnHit,
                    InitialDelayTicks = int.MaxValue
                }
            ]
        });

        _ = RunAllocationFixture([]);
        _ = RunAllocationFixture([delayedListener]);

        var unobservedAllocation = RunAllocationFixture([]);
        var observedAllocation = RunAllocationFixture([delayedListener]);

        Assert.True(
            observedAllocation > unobservedAllocation + 5_000,
            $"Expected observed events to allocate materially more. Unobserved={unobservedAllocation}, observed={observedAllocation}.");
    }

    [Fact]
    public void Starting_status_registers_hot_event_listener()
    {
        var status = AbilityCompiler.CompileStatus(new StatusSpec
        {
            Id = "status.starting.health.listener",
            Name = "Starting Health Listener",
            DurationTicks = 100,
            Triggers = [new() { Event = AbilityTriggerEvent.OnHealthChanged }],
            Effects =
            [
                new()
                {
                    Id = "effect.starting.health.listener",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 7
                }
            ]
        });
        var selfDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.starting.status.self.damage",
            Kind = AbilitySpecKind.Active,
            Name = "Self Damage",
            Effects =
            [
                new()
                {
                    Id = "effect.starting.status.self.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 5
                }
            ]
        });
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [selfDamage]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Statuses.Add(new RuntimeStatus(status, friendly, friendly, 1));
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus> { [status.Id] = status },
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        engine.Run([friendly], [hostile]);

        Assert.Equal(7, friendly.Barrier);
    }

    [Fact]
    public void Dynamically_applied_status_observes_next_hot_event()
    {
        var status = AbilityCompiler.CompileStatus(new StatusSpec
        {
            Id = "status.dynamic.hit.listener",
            Name = "Dynamic Hit Listener",
            DurationTicks = 100,
            Triggers = [new() { Event = AbilityTriggerEvent.OnHit }],
            Effects =
            [
                new()
                {
                    Id = "effect.dynamic.hit.listener",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 11
                }
            ]
        });
        var applyStatus = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.apply.dynamic.listener",
            Kind = AbilitySpecKind.Passive,
            Name = "Apply Dynamic Listener",
            Triggers = [new() { Event = AbilityTriggerEvent.OnBasicAttack }],
            Effects =
            [
                new()
                {
                    Id = "effect.apply.dynamic.listener",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.Self,
                    StatusId = status.Id,
                    BaseValue = 1
                }
            ]
        });
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [applyStatus]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus> { [status.Id] = status },
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.True(
            friendly.Statuses.Any(item => item.Definition.Id == status.Id),
            string.Join(Environment.NewLine, result.EventLog.Select(item => $"{item.Source}: {item.EventType}")));
        Assert.Contains(result.EventLog, item =>
            item.Source == "effect.dynamic.hit.listener"
            && item.EventType == EventType.RestoreBarrier
            && item.Magnitude == 11);
    }

    [Fact]
    public void Newly_created_summon_observes_hot_event_immediately()
    {
        var summonListener = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.summon.basic.listener",
            Kind = AbilitySpecKind.Passive,
            Name = "Summon Basic Listener",
            Triggers = [new() { Event = AbilityTriggerEvent.OnBasicAttack }],
            Effects =
            [
                new()
                {
                    Id = "effect.summon.basic.listener",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 13
                }
            ]
        });
        var summonAbility = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.create.listener.summon",
            Kind = AbilitySpecKind.Passive,
            Name = "Create Listener Summon",
            Effects =
            [
                new()
                {
                    Id = "effect.create.listener.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "listenerSummon",
                    DurationTicks = 100
                }
            ]
        });
        var abilities = new Dictionary<string, CompiledAbility>(StringComparer.OrdinalIgnoreCase)
        {
            [summonListener.Id] = summonListener,
            [summonAbility.Id] = summonAbility
        };
        var summons = AbilityCompiler.CompileSummons(
        [
            new SummonSpec
            {
                Id = "listenerSummon",
                Name = "Listener Summon",
                DurationTicks = 100,
                MaxActive = 1,
                AbilityIds = [summonListener.Id],
                Attributes =
                [
                    new() { Attribute = AttributeType.MaxHealth, BaseValue = 50, MinimumValue = 1 },
                    new() { Attribute = AttributeType.Power, BaseValue = 1 }
                ]
            }
        ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [summonAbility]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            summons,
            abilities,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Contains(result.EventLog, item =>
            item.Source == "effect.summon.basic.listener"
            && item.EventType == EventType.RestoreBarrier
            && item.Magnitude == 13);
    }

    private static long RunAllocationFixture(IReadOnlyList<CompiledAbility> abilities)
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities, maxHealth: 1_000_000);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 1_000_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 256,
                BasicAttackIntervalTicks: 1,
                CaptureEventLog: false));

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = engine.Run([friendly], [hostile]);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(result);
        return allocated;
    }

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        int maxHealth = 200) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = 50,
                [AttributeType.CritDamage] = 100,
                [AttributeType.AttackSpeed] = 0
            },
            abilities,
            ["Role.Test"]);
}
