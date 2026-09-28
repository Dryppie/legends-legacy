using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class TenacityCombatTests
{
    public static TheoryData<StandardConditionType, int> HarmfulConditions => new()
    {
        { StandardConditionType.Slow, 100 }, { StandardConditionType.Weaken, 100 },
        { StandardConditionType.Wound, 30 }, { StandardConditionType.Decay, 30 },
        { StandardConditionType.Poison, 120 }, { StandardConditionType.Burn, 40 },
        { StandardConditionType.Bleed, 80 }, { StandardConditionType.Stun, 30 },
        { StandardConditionType.Chill, 100 }, { StandardConditionType.Freeze, 30 },
        { StandardConditionType.Corrosion, 120 }, { StandardConditionType.Doom, 150 },
        { StandardConditionType.Mark, 30 }, { StandardConditionType.Silence, 30 },
        { StandardConditionType.Exposed, 100 }, { StandardConditionType.Vulnerable, 0 },
        { StandardConditionType.Soaked, 0 }
    };

    [Theory]
    [MemberData(nameof(HarmfulConditions))]
    public void Harmful_conditions_are_ignored_or_land_at_full_strength(StandardConditionType condition, int duration)
    {
        foreach (var seed in new[] { 0, 1 })
        {
            var listener = new AbilitySpec
            {
                Id = "listener", Name = "Listener", Kind = AbilitySpecKind.Passive,
                Triggers = [new() { Event = AbilityTriggerEvent.OnStatusApplied }],
                Effects = [new() { Id = "reward", Operation = AbilityEffectOperation.ModifyAttribute,
                    Attribute = AttributeType.Power, Target = AbilityTargetSelector.Self, BaseValue = 7 }]
            };
            var actor = Actor("actor", CombatTeam.Friendly, [Active(Condition(condition)), listener]);
            var target = Actor("target", CombatTeam.Hostile, [], 50);
            var result = Run(actor, target, seed);
            var resisted = seed == 1;

            Assert.Equal(resisted, target.Conditions.Count == 0);
            Assert.Equal(resisted ? 0 : 7, actor.GetAttribute(AttributeType.Power));
            Assert.Equal(resisted ? 1 : 0, result.EventLog.Count(x => x.EventType == EventType.StatusEffectResisted));
            Assert.Equal(resisted ? 1 : 0, result.EntityStats.Single(x => x.EntityId == target.Id).HarmfulApplicationsResisted);
            Assert.Equal(0, result.EntityStats.Single(x => x.EntityId == target.Id).HarmfulDurationTicksPrevented);
            if (!resisted)
            {
                var applied = Assert.Single(target.Conditions);
                Assert.Equal(duration, applied.DurationTicks);
                Assert.Equal(1, applied.DamageMultiplier);
                Assert.Equal(condition is StandardConditionType.Slow or StandardConditionType.Weaken
                    or StandardConditionType.Stun or StandardConditionType.Freeze or StandardConditionType.Silence
                    or StandardConditionType.Mark or StandardConditionType.Exposed or StandardConditionType.Wound
                    or StandardConditionType.Decay ? 1 : 3, applied.Value);
            }
        }
    }

    [Theory]
    [InlineData("Status.Debuff", true)]
    [InlineData("Status.Affliction", true)]
    [InlineData("Control.Stun", true)]
    [InlineData("Debuff", true)]
    [InlineData("Affliction", true)]
    [InlineData("Status.Buff", false)]
    public void Custom_status_applications_use_the_same_roll_and_keep_full_duration(string tag, bool harmful)
    {
        var status = new StatusSpec { Id = "status.test", Name = "Test", Tags = [tag], DurationTicks = 100, MaxStacks = 5 };
        var statuses = AbilityCompiler.CompileStatuses([status]);
        foreach (var seed in new[] { 0, 1 })
        {
            var actor = Actor("actor", CombatTeam.Friendly, [Active(Status(status.Id))]);
            var target = Actor("target", CombatTeam.Hostile, [], 50);
            var result = Run(actor, target, seed, statuses);
            if (harmful && seed == 1)
            {
                Assert.Empty(target.Statuses);
                Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.StatusEffect);
            }
            else
            {
                var applied = Assert.Single(target.Statuses);
                Assert.Equal(100, applied.DurationTicks);
                Assert.Equal(99, applied.RemainingDurationTicks);
                Assert.Equal(3, applied.Stacks);
            }
        }
    }

    [Theory]
    [InlineData(StandardConditionType.Slow)]
    [InlineData(StandardConditionType.Chill)]
    [InlineData(StandardConditionType.Poison)]
    [InlineData(StandardConditionType.Doom)]
    [InlineData(StandardConditionType.Vulnerable)]
    public void Resisted_reapplication_neither_refreshes_nor_adds_stacks(StandardConditionType condition)
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Active(Condition(condition))]);
        var target = Actor("target", CombatTeam.Hostile, [], 50);
        var existing = new RuntimeCondition(condition, actor, target, 2, 30, 100, 1, "existing", storedDamage: 250);
        target.Conditions.Add(existing);

        Run(actor, target, seed: 1);

        Assert.Same(existing, Assert.Single(target.Conditions));
        Assert.Equal(2, existing.Value);
        Assert.Equal(29, existing.RemainingDurationTicks);
        Assert.Equal(250, existing.StoredDamage);
    }

    [Theory]
    [InlineData(AbilityStatusStackingPolicy.Refresh)]
    [InlineData(AbilityStatusStackingPolicy.Stack)]
    [InlineData(AbilityStatusStackingPolicy.Replace)]
    public void Resisted_status_reapplication_preserves_existing_status(AbilityStatusStackingPolicy policy)
    {
        var status = new StatusSpec { Id = "debuff", Name = "Debuff", Tags = ["Status.Debuff"],
            DurationTicks = 100, MaxStacks = 5, StackingPolicy = policy };
        var statuses = AbilityCompiler.CompileStatuses([status]);
        var actor = Actor("actor", CombatTeam.Friendly, [Active(Status(status.Id))]);
        var target = Actor("target", CombatTeam.Hostile, [], 50);
        var existing = new RuntimeStatus(statuses[status.Id], actor, target, 2, durationTicks: 30)
            { CastDamageMultiplier = 1.8 };
        target.Statuses.Add(existing);

        Run(actor, target, 1, statuses);

        Assert.Same(existing, Assert.Single(target.Statuses));
        Assert.Equal(2, existing.Stacks);
        Assert.Equal(29, existing.RemainingDurationTicks);
        Assert.Equal(1.8, existing.CastDamageMultiplier);
    }

    [Fact]
    public void Resisted_status_toggle_leaves_the_previous_state_intact()
    {
        var previous = new StatusSpec { Id = "previous", Name = "Previous", DurationTicks = 100, Tags = ["Status.Buff"] };
        var next = new StatusSpec { Id = "next", Name = "Next", DurationTicks = 100, Tags = ["Status.Debuff"] };
        var statuses = AbilityCompiler.CompileStatuses([previous, next]);
        var toggle = new AbilityEffectSpec { Id = "toggle", Operation = AbilityEffectOperation.ToggleStatus,
            StatusId = previous.Id, AlternativeStatusId = next.Id, Target = AbilityTargetSelector.CurrentTarget };
        var actor = Actor("actor", CombatTeam.Friendly, [Active(toggle)]);
        var target = Actor("target", CombatTeam.Hostile, [], 50);
        var existing = new RuntimeStatus(statuses[previous.Id], actor, target, 1);
        target.Statuses.Add(existing);

        var result = Run(actor, target, 1, statuses);

        Assert.Same(existing, Assert.Single(target.Statuses));
        Assert.Contains(result.EventLog, x => x.EventType == EventType.StatusEffectResisted);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.StatusEffectRemoved);
    }

    [Fact]
    public void Zero_tenacity_never_resists_and_overcap_is_identical_to_eighty_percent()
    {
        var counts = new Dictionary<float, int>();
        foreach (var tenacity in new[] { 0f, 80f, 1000f })
        {
            var ignored = 0;
            for (var seed = 0; seed < 100; seed++)
            {
                var actor = Actor("actor", CombatTeam.Friendly, [Active(Condition(StandardConditionType.Poison))]);
                var target = Actor("target", CombatTeam.Hostile, [], tenacity);
                Run(actor, target, seed);
                if (target.Conditions.Count == 0) ignored++;
            }
            counts[tenacity] = ignored;
        }
        Assert.Equal(0, counts[0]);
        Assert.InRange(counts[80], 70, 90);
        Assert.Equal(counts[80], counts[1000]);
    }

    [Theory]
    [InlineData(StandardConditionType.Empower)]
    [InlineData(StandardConditionType.Haste)]
    [InlineData(StandardConditionType.Guard)]
    [InlineData(StandardConditionType.Renewal)]
    [InlineData(StandardConditionType.Taunt)]
    public void Beneficial_and_attention_conditions_are_not_resisted(StandardConditionType condition)
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Active(Condition(condition))]);
        var target = Actor("target", CombatTeam.Hostile, [], 80);
        Run(actor, target, seed: 1);
        Assert.Equal(condition, Assert.Single(target.Conditions).Type);
    }

    [Fact]
    public void Boss_stagger_is_applied_without_a_tenacity_roll()
    {
        var stun = Condition(StandardConditionType.Stun);
        stun.StaggerPower = 100;
        var actor = Actor("actor", CombatTeam.Friendly, [Active(stun)]);
        var target = Actor("target", CombatTeam.Hostile, [], 80, stagger: new()
            { Enabled = true, BaseThreshold = 100, BreakDurationTicks = 30, RecoveryDurationTicks = 20 });

        var result = Run(actor, target, seed: 1);

        Assert.Empty(target.Conditions);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.StaggerBroken);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.StatusEffectResisted);
    }

    [Fact]
    public void Guaranteed_application_bypasses_tenacity_and_existing_ward_and_control_immunity()
    {
        var stun = Condition(StandardConditionType.Stun);
        stun.GuaranteedConditionApplication = true;
        var actor = Actor("actor", CombatTeam.Friendly, [Active(stun)]);
        var target = Actor("target", CombatTeam.Hostile, [], 80);
        target.Conditions.Add(new(StandardConditionType.Ward, target, target, 1, 0, 0, 1, "ward"));
        target.Conditions.Add(new(StandardConditionType.Unstoppable, target, target, 1, 100, 0, 2, "immunity"));

        var result = Run(actor, target, seed: 1);

        Assert.Equal(30, Assert.Single(target.Conditions, x => x.Type == StandardConditionType.Stun).DurationTicks);
        Assert.True(target.HasCondition(StandardConditionType.Ward));
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.StatusEffectResisted);
    }

    [Theory]
    [InlineData(24.8f, false)]
    [InlineData(24.9f, true)]
    public void Fractional_tenacity_is_not_rounded_to_whole_percent(float tenacity, bool resisted)
    {
        var actor = Actor("actor", CombatTeam.Friendly, [Active(Condition(StandardConditionType.Poison))]);
        var target = Actor("target", CombatTeam.Hostile, [], tenacity);
        Run(actor, target, seed: 1);
        Assert.Equal(resisted, target.Conditions.Count == 0);
    }

    [Fact]
    public void Repeated_applications_roll_individually_in_the_same_cast()
    {
        var poison = Condition(StandardConditionType.Poison);
        poison.RepeatCount = 6;
        var actor = Actor("actor", CombatTeam.Friendly, [Active(poison)]);
        var target = Actor("target", CombatTeam.Hostile, [], 50);

        var result = Run(actor, target, seed: 1);

        var ignored = result.EventLog.Count(x => x.EventType == EventType.StatusEffectResisted);
        Assert.InRange(ignored, 1, 5);
        Assert.Equal(6, ignored + target.Conditions.Count);
    }

    [Fact]
    public void Legacy_combat_keeps_duration_resistance_and_does_not_roll_tenacity()
    {
        var status = new StatusSpec { Id = "debuff", Name = "Debuff", DurationTicks = 100, Tags = ["Debuff"] };
        var actor = Actor("actor", CombatTeam.Friendly, [Active(Status(status.Id)), Active(Condition(StandardConditionType.Poison))]);
        var target = new RuntimeCombatant("target", "target", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000,
                [AttributeType.StatusResistance] = 50, [AttributeType.Tenacity] = 80 }, [], canBasicAttack: false,
            attributeRulesVersion: AttributeRules.LegacyVersion);

        var result = Run(actor, target, 1, AbilityCompiler.CompileStatuses([status]));

        Assert.Equal(50, Assert.Single(target.Statuses).DurationTicks);
        Assert.Equal(120, Assert.Single(target.Conditions).DurationTicks);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.StatusEffectResisted);
    }

    private static AbilityEffectSpec Condition(StandardConditionType condition) => new()
    {
        Id = "condition", Operation = AbilityEffectOperation.ApplyCondition, Condition = condition,
        Target = AbilityTargetSelector.CurrentTarget, BaseValue = 3
    };

    private static AbilityEffectSpec Status(string id) => new()
    {
        Id = "status", Operation = AbilityEffectOperation.ApplyStatus, StatusId = id,
        Target = AbilityTargetSelector.CurrentTarget, BaseValue = 3
    };

    private static AbilitySpec Active(AbilityEffectSpec effect) => new()
        { Id = effect.Id, Name = effect.Id, Kind = AbilitySpecKind.Active, CooldownTicks = 100, Effects = [effect] };

    private static RuntimeCombatant Actor(string id, CombatTeam team, AbilitySpec[] abilities, float tenacity = 0,
        BossStaggerDefinition? stagger = null) => new(id, id, team,
        new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000, [AttributeType.Tenacity] = tenacity },
        abilities.Select(AbilityCompiler.CompileAbility), canBasicAttack: false, staggerDefinition: stagger,
        attributeRulesVersion: AttributeRules.CurrentVersion);

    private static CombatResult Run(RuntimeCombatant actor, RuntimeCombatant target, int seed,
        IReadOnlyDictionary<string, CompiledStatus>? statuses = null) =>
        new FastCombatEngine(statuses ?? new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: seed)).Run([actor], [target]);
}
