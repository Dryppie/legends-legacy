using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class StandardConditionSystemTests
{
    [Fact]
    public void Ability_catalog_accepts_typed_standard_condition_effects_and_queries()
    {
        var ability = Passive(
            "condition.authoring",
            new AbilityEffectSpec
            {
                Id = "apply.poison",
                Operation = AbilityEffectOperation.ApplyCondition,
                Condition = StandardConditionType.Poison,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = 3
            });
        ability.Effects.Add(
            new AbilityEffectSpec
            {
                Id = "conditional.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = 10,
                Conditions =
                [
                    new AbilityConditionSpec
                    {
                        Type = AbilityConditionType.ConditionStacksAtLeast,
                        Subject = AbilityConditionSubject.Target,
                        Condition = StandardConditionType.Poison,
                        Value = 3
                    }
                ]
            });

        var validation = AbilityCatalogValidator.Validate([ability], []);

        Assert.True(validation.IsValid, string.Join(" | ", validation.Errors));
        var compiled = AbilityCompiler.CompileAbility(ability);
        Assert.Equal(
            StandardConditionType.Poison,
            compiled.TriggersByEvent[AbilityTriggerEvent.OnCombatStart][0].Effects[0].Condition);
    }

    [Fact]
    public void Wound_and_recovery_use_independent_timers_but_one_fixed_modifier()
    {
        var woundOnly = Active(
            "wound.heal",
            healthCost: 100,
            ConditionEffect("wound.one", StandardConditionType.Wound, AbilityTargetSelector.Self, 6),
            ConditionEffect("wound.two", StandardConditionType.Wound, AbilityTargetSelector.Self, 10),
            HealEffect("heal", 100));
        var actor = Combatant("actor", CombatTeam.Friendly, [woundOnly], maxHealth: 200);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 1);

        Assert.Equal(170, actor.Health);
        Assert.Equal(2, actor.Conditions.Count(x => x.Type == StandardConditionType.Wound));

        var canceled = Active(
            "wound.recovery.heal",
            healthCost: 100,
            ConditionEffect("wound", StandardConditionType.Wound, AbilityTargetSelector.Self, 6),
            ConditionEffect("recovery", StandardConditionType.Recovery, AbilityTargetSelector.Self, 6),
            HealEffect("heal", 100));
        var canceledActor = Combatant("canceled", CombatTeam.Friendly, [canceled], maxHealth: 200);
        var canceledEnemy = Combatant("canceled.enemy", CombatTeam.Hostile, []);

        Run([canceledActor], [canceledEnemy], maxTicks: 1);

        Assert.Equal(200, canceledActor.Health);
    }

    [Fact]
    public void Decay_and_renewal_modify_regeneration_amount_without_changing_interval()
    {
        var setup = Passive(
            "regeneration.setup",
            ConditionEffect("decay", StandardConditionType.Decay, AbilityTargetSelector.Self, 10),
            new AbilityEffectSpec
            {
                Id = "self.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.Self,
                BaseValue = 100,
                DamageType = DamageType.None,
                CritEligibility = CritEligibility.Disallowed
            });
        var actor = Combatant(
            "actor",
            CombatTeam.Friendly,
            [setup],
            maxHealth: 200,
            healthRegeneration: 100);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 50);

        Assert.Equal(170, actor.Health);

        var pairedSetup = Passive(
            "regeneration.paired",
            ConditionEffect("decay", StandardConditionType.Decay, AbilityTargetSelector.Self, 10),
            ConditionEffect("renewal", StandardConditionType.Renewal, AbilityTargetSelector.Self, 10),
            new AbilityEffectSpec
            {
                Id = "self.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.Self,
                BaseValue = 100,
                DamageType = DamageType.None,
                CritEligibility = CritEligibility.Disallowed
            });
        var pairedActor = Combatant(
            "paired",
            CombatTeam.Friendly,
            [pairedSetup],
            maxHealth: 200,
            healthRegeneration: 100);
        var pairedEnemy = Combatant("paired.enemy", CombatTeam.Hostile, []);

        Run([pairedActor], [pairedEnemy], maxTicks: 50);

        Assert.Equal(200, pairedActor.Health);
    }

    [Fact]
    public void Vulnerable_guard_and_corrosion_resolve_in_canonical_order()
    {
        var attack = Passive(
            "canonical.damage.order",
            ConditionEffect("guard", StandardConditionType.Guard, AbilityTargetSelector.CurrentTarget, 1),
            ConditionEffect("vulnerable", StandardConditionType.Vulnerable, AbilityTargetSelector.CurrentTarget, 2),
            ConditionEffect("corrosion", StandardConditionType.Corrosion, AbilityTargetSelector.CurrentTarget, 50),
            new AbilityEffectSpec
            {
                Id = "hit",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = 100,
                AttackType = AttackType.Melee,
                DamageType = DamageType.Physical,
                CritEligibility = CritEligibility.Disallowed
            });
        var actor = Combatant("actor", CombatTeam.Friendly, [attack]);
        var enemy = Combatant("enemy", CombatTeam.Hostile, [], armor: 100);

        Run([actor], [enemy], maxTicks: 1);

        Assert.Equal(954, enemy.Health);
        Assert.False(enemy.HasCondition(StandardConditionType.Guard));
        Assert.Equal(1, enemy.GetConditionStacks(StandardConditionType.Vulnerable));
        Assert.Equal(50, enemy.GetConditionStacks(StandardConditionType.Corrosion));
    }

    [Fact]
    public void Vulnerable_amplifies_and_consumes_one_stack_per_direct_hit()
    {
        var attack = Passive(
            "vulnerable.charges",
            ConditionEffect("vulnerable", StandardConditionType.Vulnerable, AbilityTargetSelector.CurrentTarget, 2),
            DamageEffect("first.hit", 100),
            DamageEffect("second.hit", 100),
            DamageEffect("third.hit", 100));
        var actor = Combatant("actor", CombatTeam.Friendly, [attack]);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 1);

        Assert.Equal(650, enemy.Health);
        Assert.False(enemy.HasCondition(StandardConditionType.Vulnerable));
    }

    [Fact]
    public void Periodic_damage_does_not_amplify_or_consume_vulnerable()
    {
        var conditions = Passive(
            "vulnerable.periodic",
            ConditionEffect("vulnerable", StandardConditionType.Vulnerable, AbilityTargetSelector.CurrentTarget, 1),
            ConditionEffect("poison", StandardConditionType.Poison, AbilityTargetSelector.CurrentTarget, 10));
        var actor = Combatant("actor", CombatTeam.Friendly, [conditions], power: 100);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 21);

        Assert.Equal(990, enemy.Health);
        Assert.Equal(1, enemy.GetConditionStacks(StandardConditionType.Vulnerable));
    }

    [Fact]
    public void Poison_burn_bleed_and_doom_snapshot_power_and_keep_independent_schedules()
    {
        var conditions = Passive(
            "periodic.conditions",
            ConditionEffect("poison", StandardConditionType.Poison, AbilityTargetSelector.CurrentTarget, 3),
            ConditionEffect("burn", StandardConditionType.Burn, AbilityTargetSelector.CurrentTarget, 2),
            ConditionEffect("bleed", StandardConditionType.Bleed, AbilityTargetSelector.CurrentTarget, 4),
            ConditionEffect("doom", StandardConditionType.Doom, AbilityTargetSelector.CurrentTarget, 40));
        var actor = Combatant("actor", CombatTeam.Friendly, [conditions], power: 100);
        var enemy = Combatant("enemy", CombatTeam.Hostile, [], maxHealth: 2000);

        Run([actor], [enemy], maxTicks: 150);

        var periodicDamage = 3 * 6 + 2 * 4 + 4 * 4;
        Assert.Equal(2000 - periodicDamage - 40, enemy.Health);
        Assert.DoesNotContain(
            enemy.Conditions,
            x => x.Type is StandardConditionType.Poison
                or StandardConditionType.Burn
                or StandardConditionType.Bleed
                or StandardConditionType.Doom);
    }

    [Fact]
    public void Ward_blocks_whole_application_and_unstoppable_blocks_control_before_ward()
    {
        var wardThenPoison = Passive(
            "ward.poison",
            ConditionEffect("ward", StandardConditionType.Ward, AbilityTargetSelector.CurrentTarget, 1),
            ConditionEffect("poison", StandardConditionType.Poison, AbilityTargetSelector.CurrentTarget, 3));
        var actor = Combatant("actor", CombatTeam.Friendly, [wardThenPoison]);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 1);

        Assert.False(enemy.HasCondition(StandardConditionType.Ward));
        Assert.False(enemy.HasCondition(StandardConditionType.Poison));

        var immunity = Passive(
            "unstoppable.stun",
            ConditionEffect("unstoppable", StandardConditionType.Unstoppable, AbilityTargetSelector.CurrentTarget, 5),
            ConditionEffect("ward", StandardConditionType.Ward, AbilityTargetSelector.CurrentTarget, 1),
            ConditionEffect("stun", StandardConditionType.Stun, AbilityTargetSelector.CurrentTarget, 3));
        var immuneActor = Combatant("immune.actor", CombatTeam.Friendly, [immunity]);
        var immuneEnemy = Combatant("immune.enemy", CombatTeam.Hostile, []);

        Run([immuneActor], [immuneEnemy], maxTicks: 1);

        Assert.True(immuneEnemy.HasCondition(StandardConditionType.Unstoppable));
        Assert.True(immuneEnemy.HasCondition(StandardConditionType.Ward));
        Assert.False(immuneEnemy.HasCondition(StandardConditionType.Stun));
    }

    [Fact]
    public void Thorns_sums_independent_stacks_and_reflection_does_not_recurse()
    {
        var thorns = Passive(
            "thorns",
            ConditionEffect("thorns.20", StandardConditionType.Thorns, AbilityTargetSelector.Self, 20, durationTicks: 80),
            ConditionEffect("thorns.15", StandardConditionType.Thorns, AbilityTargetSelector.Self, 15, durationTicks: 40));
        var attacker = Combatant(
            "attacker",
            CombatTeam.Friendly,
            [],
            maxHealth: 1000,
            power: 0,
            weaponDamage: 100);
        var defender = Combatant(
            "defender",
            CombatTeam.Hostile,
            [thorns],
            maxHealth: 1000,
            basicAttackIntervalMultiplier: 1000);

        Run([attacker], [defender], maxTicks: 1, basicAttackIntervalTicks: 1);

        var damageDealt = 1000 - defender.Health;
        Assert.InRange(damageDealt, 80, 120);
        Assert.Equal((int)Math.Round(damageDealt * 0.35), 1000 - attacker.Health);
        Assert.Equal(2, defender.Conditions.Count(x => x.Type == StandardConditionType.Thorns));
    }

    [Fact]
    public void Barrier_is_capped_tracks_sources_and_consumes_oldest_first()
    {
        var first = Combatant("first", CombatTeam.Friendly, []);
        var second = Combatant("second", CombatTeam.Friendly, []);
        var target = Combatant("target", CombatTeam.Friendly, [], maxHealth: 100);

        Assert.Equal(200, target.GrantBarrier(first, 200, 1));
        Assert.Equal(50, target.GrantBarrier(second, 200, 2));
        Assert.Equal(250, target.Barrier);

        Assert.Equal(225, target.ConsumeBarrier(225));
        Assert.Equal(25, target.Barrier);
        Assert.Single(target.BarrierContributions);
        Assert.Same(second, target.BarrierContributions[0].Source);
    }

    [Fact]
    public void Barrier_absorption_events_preserve_each_contribution_source()
    {
        var first = Combatant("first.source", CombatTeam.Friendly, []);
        var second = Combatant("second.source", CombatTeam.Friendly, []);
        var attacker = Combatant(
            "attacker",
            CombatTeam.Friendly,
            [],
            power: 0,
            weaponDamage: 10);
        var target = Combatant(
            "target",
            CombatTeam.Hostile,
            [],
            basicAttackIntervalMultiplier: 1000);
        target.GrantBarrier(first, 4, 1);
        target.GrantBarrier(second, 6, 2);

        var result = Run([attacker], [target], maxTicks: 1, basicAttackIntervalTicks: 1);

        var absorbed = result.EventLog
            .Where(x => x.EventType == EventType.BarrierAbsorbed)
            .ToList();
        Assert.Collection(
            absorbed,
            item =>
            {
                Assert.Equal("first.source", item.ActorId);
                Assert.Equal("target", item.TargetId);
                Assert.Equal(4, item.Magnitude);
            },
            item =>
            {
                Assert.Equal("second.source", item.ActorId);
                Assert.Equal("target", item.TargetId);
                Assert.Equal(6, item.Magnitude);
            });
        Assert.Contains(
            result.EventLog,
            x => x.EventType == EventType.BarrierBroken
                && x.ActorId == "second.source"
                && x.TargetId == "target");
    }

    [Fact]
    public void Thorns_reflects_received_damage_before_barrier_absorption()
    {
        var attacker = Combatant(
            "attacker",
            CombatTeam.Friendly,
            [],
            power: 0,
            weaponDamage: 100);
        var thorns = Passive(
            "thorns",
            ConditionEffect(
                "thorns.20",
                StandardConditionType.Thorns,
                AbilityTargetSelector.Self,
                20,
                durationTicks: 80));
        var defender = Combatant(
            "defender",
            CombatTeam.Hostile,
            [thorns],
            basicAttackIntervalMultiplier: 1000);
        defender.GrantBarrier(defender, 40, 1);

        var result = Run([attacker], [defender], maxTicks: 1, basicAttackIntervalTicks: 1);

        var healthDamage = 1000 - defender.Health;
        Assert.InRange(healthDamage, 40, 80);
        var incomingDamage = healthDamage + 40;
        Assert.Equal((int)Math.Round(incomingDamage * 0.20), 1000 - attacker.Health);

        var defenderStats = Assert.Single(result.EntityStats, x => x.EntityId == "defender");
        var thornsStats = Assert.Single(defenderStats.Abilities, x => x.Name == "thorns");
        Assert.Equal(1000 - attacker.Health, thornsStats.TotalDamage);
    }

    [Fact]
    public void Thorns_reflects_when_barrier_absorbs_the_entire_hit()
    {
        var attacker = Combatant(
            "attacker",
            CombatTeam.Friendly,
            [],
            power: 0,
            weaponDamage: 100);
        var thorns = Passive(
            "thorns",
            ConditionEffect(
                "thorns.20",
                StandardConditionType.Thorns,
                AbilityTargetSelector.Self,
                20,
                durationTicks: 80));
        var defender = Combatant(
            "defender",
            CombatTeam.Hostile,
            [thorns],
            basicAttackIntervalMultiplier: 1000);
        defender.GrantBarrier(defender, 1000, 1);

        Run([attacker], [defender], maxTicks: 1, basicAttackIntervalTicks: 1);

        Assert.Equal(1000, defender.Health);
        Assert.InRange(1000 - attacker.Health, 16, 24);
    }

    [Fact]
    public void Interval_trigger_fires_immediately_and_repeats_on_internal_cooldown()
    {
        var interval = new AbilitySpec
        {
            Id = "interval",
            Name = "interval",
            Kind = AbilitySpecKind.Passive,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnInterval,
                    InternalCooldownTicks = 3,
                    EffectIds = ["interval.barrier"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "interval.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 1
                }
            ]
        };
        var actor = Combatant("actor", CombatTeam.Friendly, [interval]);
        var enemy = Combatant(
            "enemy",
            CombatTeam.Hostile,
            [],
            basicAttackIntervalMultiplier: 1000);

        var result = Run([actor], [enemy], maxTicks: 7);

        Assert.Equal(3, result.EventLog.Count(
            x => x.Source == "interval.barrier"
                && x.EventType == EventType.RestoreBarrier));
        Assert.Equal(3, actor.Barrier);
    }

    [Fact]
    public void Cleanse_and_dispel_publish_distinct_removal_events()
    {
        var lifecycle = new AbilitySpec
        {
            Id = "removal.events",
            Name = "removal.events",
            Kind = AbilitySpecKind.Passive,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnCombatStart,
                    EffectIds = ["apply.wound", "cleanse.wound", "apply.empower", "dispel.empower"]
                },
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnStatusCleansed,
                    EffectIds = ["cleanse.marker"]
                },
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnStatusDispelled,
                    EffectIds = ["dispel.marker"]
                }
            ],
            Effects =
            [
                ConditionEffect(
                    "apply.wound",
                    StandardConditionType.Wound,
                    AbilityTargetSelector.Self,
                    5),
                new AbilityEffectSpec
                {
                    Id = "cleanse.wound",
                    Operation = AbilityEffectOperation.Cleanse,
                    Condition = StandardConditionType.Wound,
                    Target = AbilityTargetSelector.Self
                },
                ConditionEffect(
                    "apply.empower",
                    StandardConditionType.Empower,
                    AbilityTargetSelector.Self,
                    1),
                new AbilityEffectSpec
                {
                    Id = "dispel.empower",
                    Operation = AbilityEffectOperation.Dispel,
                    Condition = StandardConditionType.Empower,
                    Target = AbilityTargetSelector.Self
                },
                new AbilityEffectSpec
                {
                    Id = "cleanse.marker",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 3
                },
                new AbilityEffectSpec
                {
                    Id = "dispel.marker",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 5
                }
            ]
        };
        var actor = Combatant("actor", CombatTeam.Friendly, [lifecycle]);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        var result = Run([actor], [enemy], maxTicks: 1);

        Assert.Equal(8, actor.Barrier);
        Assert.Contains(
            result.EventLog,
            x => x.Source == "condition.wound"
                && x.EventType == EventType.StatusEffectCleansed);
        Assert.Contains(
            result.EventLog,
            x => x.Source == "condition.empower"
                && x.EventType == EventType.StatusEffectDispelled);
        Assert.DoesNotContain(
            result.EventLog,
            x => x.Source is "condition.wound" or "condition.empower"
                && x.EventType == EventType.StatusEffectExpired);
    }

    [Fact]
    public void Taunt_default_threat_bonus_is_one_hundred()
    {
        Assert.Equal(100f, new FastCombatEngineOptions().TauntThreatBonus);
    }

    [Fact]
    public void All_combatants_default_to_one_hundred_threat()
    {
        var owner = Combatant("owner", CombatTeam.Hostile, []);
        var summon = new RuntimeCombatant(
            "summon",
            "Summon",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1000
            },
            [],
            isSummoned: true);

        Assert.Equal(RuntimeCombatant.BaseThreat, owner.Threat);
        Assert.Equal(RuntimeCombatant.BaseThreat, summon.Threat);
    }

    [Fact]
    public void Empower_weaken_haste_slow_and_chill_apply_their_fixed_independent_modifiers()
    {
        var empoweredStrike = Passive(
            "empowered.strike",
            ConditionEffect("empower", StandardConditionType.Empower, AbilityTargetSelector.Self, 1),
            new AbilityEffectSpec
            {
                Id = "power.hit",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                ScalingAttribute = AttributeType.Power,
                ScalingCoefficient = 1,
                DamageType = DamageType.None,
                CritEligibility = CritEligibility.Disallowed
            });
        var empowered = Combatant("empowered", CombatTeam.Friendly, [empoweredStrike], power: 100);
        var empoweredTarget = Combatant("empowered.target", CombatTeam.Hostile, []);

        Run([empowered], [empoweredTarget], maxTicks: 1);
        var empoweredDamage = 1000 - empoweredTarget.Health;

        var canceledStrike = Passive(
            "canceled.strike",
            ConditionEffect("empower", StandardConditionType.Empower, AbilityTargetSelector.Self, 1),
            ConditionEffect("weaken", StandardConditionType.Weaken, AbilityTargetSelector.Self, 1),
            new AbilityEffectSpec
            {
                Id = "power.hit",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                ScalingAttribute = AttributeType.Power,
                ScalingCoefficient = 1,
                DamageType = DamageType.None,
                CritEligibility = CritEligibility.Disallowed
            });
        var canceled = Combatant("canceled", CombatTeam.Friendly, [canceledStrike], power: 100);
        var canceledTarget = Combatant("canceled.target", CombatTeam.Hostile, []);

        Run([canceled], [canceledTarget], maxTicks: 1);
        var canceledDamage = 1000 - canceledTarget.Health;

        Assert.Equal((int)Math.Round(canceledDamage * 1.20), empoweredDamage);

        var hasteAndSlow = Passive(
            "haste.slow",
            ConditionEffect("haste", StandardConditionType.Haste, AbilityTargetSelector.Self, 1),
            ConditionEffect("slow", StandardConditionType.Slow, AbilityTargetSelector.Self, 1));
        var normalRate = Combatant("normal.rate", CombatTeam.Friendly, [hasteAndSlow], power: 0, weaponDamage: 10);
        var normalTarget = Combatant(
            "normal.target",
            CombatTeam.Hostile,
            [],
            basicAttackIntervalMultiplier: 1000);

        Run([normalRate], [normalTarget], maxTicks: 4, basicAttackIntervalTicks: 4);

        Assert.InRange(1000 - normalTarget.Health, 8, 12);

        var chilled = Passive(
            "slow.chill",
            ConditionEffect("slow", StandardConditionType.Slow, AbilityTargetSelector.Self, 1),
            ConditionEffect("chill", StandardConditionType.Chill, AbilityTargetSelector.Self, 20));
        var slowRate = Combatant("slow.rate", CombatTeam.Friendly, [chilled], power: 0, weaponDamage: 10);
        var slowTarget = Combatant(
            "slow.target",
            CombatTeam.Hostile,
            [],
            basicAttackIntervalMultiplier: 1000);

        Run([slowRate], [slowTarget], maxTicks: 4, basicAttackIntervalTicks: 4);

        Assert.Equal(1000, slowTarget.Health);
        Assert.Equal(20, slowRate.GetConditionStacks(StandardConditionType.Chill));
    }

    [Fact]
    public void Targeted_cleanse_removes_one_independent_wound_stack()
    {
        var cleanse = Passive(
            "cleanse.wound",
            ConditionEffect("wound.one", StandardConditionType.Wound, AbilityTargetSelector.Self, 6),
            ConditionEffect("wound.two", StandardConditionType.Wound, AbilityTargetSelector.Self, 10),
            new AbilityEffectSpec
            {
                Id = "cleanse",
                Operation = AbilityEffectOperation.Cleanse,
                Condition = StandardConditionType.Wound,
                Target = AbilityTargetSelector.Self
            });
        var actor = Combatant("actor", CombatTeam.Friendly, [cleanse]);
        var enemy = Combatant("enemy", CombatTeam.Hostile, []);

        Run([actor], [enemy], maxTicks: 1);

        var remaining = Assert.Single(actor.Conditions, x => x.Type == StandardConditionType.Wound);
        Assert.Equal(99, remaining.RemainingDurationTicks);
    }

    [Fact]
    public void Basic_attack_lifesteal_is_eligible_and_uses_healing_received_modifiers()
    {
        var setup = Passive(
            "lifesteal.setup",
            ConditionEffect("wound", StandardConditionType.Wound, AbilityTargetSelector.Self, 5),
            new AbilityEffectSpec
            {
                Id = "self.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.Self,
                BaseValue = 100,
                DamageType = DamageType.None,
                CritEligibility = CritEligibility.Disallowed
            });
        var actor = Combatant(
            "actor",
            CombatTeam.Friendly,
            [setup],
            power: 0,
            weaponDamage: 100,
            lifeSteal: 50);
        var enemy = Combatant(
            "enemy",
            CombatTeam.Hostile,
            [],
            basicAttackIntervalMultiplier: 1000);

        Run([actor], [enemy], maxTicks: 1, basicAttackIntervalTicks: 1);

        var damageDealt = 1000 - enemy.Health;
        var expectedHealing = (int)Math.Round(Math.Round(damageDealt * 0.50) * 0.70);
        Assert.Equal(900 + expectedHealing, actor.Health);
    }

    [Fact]
    public void Threat_weighting_uses_modified_threat_and_stealth_overrides_it_to_one()
    {
        var lowThreat = Passive(
            "low.threat",
            new AbilityEffectSpec
            {
                Id = "threat",
                Operation = AbilityEffectOperation.ModifyThreat,
                Target = AbilityTargetSelector.Self,
                BaseValue = -100
            });
        var highThreat = Passive(
            "high.threat",
            new AbilityEffectSpec
            {
                Id = "threat",
                Operation = AbilityEffectOperation.ModifyThreat,
                Target = AbilityTargetSelector.Self,
                BaseValue = 100
            });
        var attacker = Combatant("attacker", CombatTeam.Friendly, [], power: 0, weaponDamage: 10);
        var low = Combatant("low", CombatTeam.Hostile, [lowThreat]);
        var high = Combatant("high", CombatTeam.Hostile, [highThreat]);

        Run([attacker], [low, high], maxTicks: 1, basicAttackIntervalTicks: 1);

        Assert.Equal(1000, low.Health);
        Assert.InRange(1000 - high.Health, 8, 12);

        var stealthThreat = Passive(
            "stealth.threat",
            new AbilityEffectSpec
            {
                Id = "threat",
                Operation = AbilityEffectOperation.ModifyThreat,
                Target = AbilityTargetSelector.Self,
                BaseValue = 100
            },
            ConditionEffect("stealth", StandardConditionType.Stealth, AbilityTargetSelector.Self, 5));
        var visibleThreat = Passive(
            "visible.threat",
            new AbilityEffectSpec
            {
                Id = "threat",
                Operation = AbilityEffectOperation.ModifyThreat,
                Target = AbilityTargetSelector.Self,
                BaseValue = 10
            });
        var secondAttacker = Combatant("second.attacker", CombatTeam.Friendly, [], power: 0, weaponDamage: 10);
        var stealth = Combatant("stealth", CombatTeam.Hostile, [stealthThreat]);
        var visible = Combatant("visible", CombatTeam.Hostile, [visibleThreat]);

        Run([secondAttacker], [stealth, visible], maxTicks: 1, basicAttackIntervalTicks: 1, randomSeed: 1337);

        Assert.Equal(1000, stealth.Health);
        Assert.InRange(1000 - visible.Health, 8, 12);
    }

    private static AbilitySpec Passive(string id, params AbilityEffectSpec[] effects) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Passive,
            Effects = [.. effects]
        };

    private static AbilitySpec Active(string id, int healthCost, params AbilityEffectSpec[] effects) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 1000,
            Costs = [new AbilityCostSpec { Resource = AbilityResourceType.Health, BaseValue = healthCost }],
            Effects = [.. effects]
        };

    private static AbilityEffectSpec ConditionEffect(
        string id,
        StandardConditionType condition,
        AbilityTargetSelector target,
        int value,
        int durationTicks = 0) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.ApplyCondition,
            Condition = condition,
            Target = target,
            BaseValue = value,
            DurationTicks = durationTicks
        };

    private static AbilityEffectSpec HealEffect(string id, int value) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.Heal,
            Target = AbilityTargetSelector.Self,
            BaseValue = value,
            CritEligibility = CritEligibility.Disallowed
        };

    private static AbilityEffectSpec DamageEffect(string id, int value) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.Damage,
            Target = AbilityTargetSelector.CurrentTarget,
            BaseValue = value,
            DamageType = DamageType.None,
            CritEligibility = CritEligibility.Disallowed
        };

    private static RuntimeCombatant Combatant(
        string id,
        CombatTeam team,
        IEnumerable<AbilitySpec> abilities,
        int maxHealth = 1000,
        int power = 100,
        int weaponDamage = 1,
        int armor = 0,
        int healthRegeneration = 0,
        int lifeSteal = 0,
        double basicAttackIntervalMultiplier = 1d) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = power,
                [AttributeType.Armor] = armor,
                [AttributeType.Resistance] = 0,
                [AttributeType.HealthRegeneration] = healthRegeneration,
                [AttributeType.LifeSteal] = lifeSteal
            },
            abilities.Select(AbilityCompiler.CompileAbility),
            basicAttackDamageMultiplier: weaponDamage,
            basicAttackIntervalMultiplier: basicAttackIntervalMultiplier);

    private static CombatResult Run(
        IReadOnlyList<RuntimeCombatant> friendly,
        IReadOnlyList<RuntimeCombatant> hostile,
        int maxTicks,
        int basicAttackIntervalTicks = 1000,
        int randomSeed = 1337) =>
        new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(
                    MaxTicks: maxTicks,
                    BasicAttackIntervalTicks: basicAttackIntervalTicks,
                    RandomSeed: randomSeed))
            .Run(friendly, hostile);
}
