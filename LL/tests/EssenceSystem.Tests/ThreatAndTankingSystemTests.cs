using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Essences;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class ThreatAndTankingSystemTests
{
    [Fact]
    public void Ability_threat_uses_authored_values_and_cadence_normalized_function_bands()
    {
        var authored = new AbilitySpec
        {
            ThreatValue = -250,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 100
                }
            ]
        };
        var damage = new AbilitySpec
        {
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 3f
                },
                new AbilityEffectSpec
                {
                    Id = "second-damage",
                    Operation = AbilityEffectOperation.Damage,
                    ScalingAttribute = AttributeType.MaxHealth,
                    ScalingCoefficient = 0.1f
                }
            ]
        };
        var support = new AbilitySpec
        {
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.AllAllies,
                    ScalingAttribute = AttributeType.MaxHealth,
                    ScalingCoefficient = 0.25f
                }
            ]
        };

        Assert.Equal(-250, AbilityThreatRules.GetThreatValue(authored));
        Assert.Equal(15, AbilityThreatRules.GetThreatValue(damage));
        Assert.Equal(50, AbilityThreatRules.GetThreatValue(support));
    }

    [Fact]
    public void Ability_progression_preserves_authored_threat_fields()
    {
        var scaled = EssenceAbilityProgressionScaler.Apply(
            new AbilitySpec
            {
                Id = "authored-threat",
                Kind = AbilitySpecKind.Active,
                CooldownTicks = 100,
                ThreatValue = -250,
                ThreatMultiplier = 1.5f
            },
            ascensionTier: 1);

        Assert.Equal(-250, scaled.ThreatValue);
        Assert.Equal(1.5f, scaled.ThreatMultiplier);
    }

    [Fact]
    public void Estimated_threat_per_second_uses_active_and_passive_cadence()
    {
        var active = new AbilitySpec
        {
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            ThreatValue = 30,
            ThreatMultiplier = 1.5f
        };
        var passive = new AbilitySpec
        {
            Kind = AbilitySpecKind.Passive,
            ThreatValue = 24,
            ThreatMultiplier = 0.5f,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnBasicAttack,
                    EveryNthOccurrence = 2
                }
            ]
        };

        Assert.Equal(4.5d, AbilityThreatRules.GetEstimatedThreatPerSecond(active), precision: 5);
        Assert.Equal(2d, AbilityThreatRules.GetEstimatedThreatPerSecond(passive), precision: 5);
    }

    [Fact]
    public void Threat_derivation_and_summon_defaults_accept_runtime_tuning()
    {
        var tuning = new AbilityThreatTuning(
            BasicAttackThreatValue: 9,
            ProtectiveSelfThreatPerSecond: 10f,
            ProtectiveAllyThreatPerSecond: 9f,
            RetaliationThreatPerSecond: 8f,
            SupportAllyThreatPerSecond: 7f,
            HardControlThreatPerSecond: 6f,
            SoftControlThreatPerSecond: 5f,
            DamageThreatPerSecond: 3f,
            SelfSustainThreatPerSecond: 2f,
            UtilityThreatPerSecond: 1f,
            DefaultSummonThreatMultiplier: 0.4f);
        var ability = new AbilitySpec
        {
            Id = "configured-threat",
            Name = "Configured Threat",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 3f
                }
            ]
        };
        var summon = new SummonSpec { Id = "configured-summon", Name = "Configured Summon" };

        Assert.Equal(30, AbilityCompiler.CompileAbility(ability, tuning).ThreatValue);
        Assert.Equal(0.4f, AbilityCompiler.CompileSummon(summon, tuning).ThreatMultiplier);
    }

    [Fact]
    public void Direct_magnitudes_reject_flat_values_but_allow_any_content_facing_scaling_attribute()
    {
        var invalid = new AbilitySpec
        {
            Id = "flat-damage",
            Name = "Flat Damage",
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 10,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f
                }
            ]
        };
        var valid = new AbilitySpec
        {
            Id = "health-barrier",
            Name = "Health Barrier",
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    ScalingAttribute = AttributeType.MaxHealth,
                    ScalingCoefficient = 0.1f
                }
            ]
        };

        var invalidResult = AbilityCatalogValidator.Validate([invalid], []);
        var validResult = AbilityCatalogValidator.Validate([valid], []);

        Assert.Contains(invalidResult.Errors, error => error.Contains("cannot use baseValue", StringComparison.Ordinal));
        Assert.True(validResult.IsValid, string.Join(" | ", validResult.Errors));
    }

    [Fact]
    public void Ungated_reactive_passives_receive_a_threat_only_cooldown()
    {
        var ability = new AbilitySpec
        {
            Id = "reactive",
            Name = "Reactive",
            Kind = AbilitySpecKind.Passive,
            Triggers = [new AbilityTriggerSpec { Event = AbilityTriggerEvent.OnDamaged }],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "retaliation",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventSource,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f
                }
            ]
        };

        var trigger = Assert.Single(AbilityCompiler.CompileAbility(ability)
            .TriggersByEvent[AbilityTriggerEvent.OnDamaged]);

        Assert.Equal(14, trigger.ThreatValue);
        Assert.Equal(40, trigger.ThreatInternalCooldownTicks);
        Assert.Equal(0, trigger.InternalCooldownTicks);
    }

    [Fact]
    public void Passive_trigger_generates_authored_threat_and_threat_decays_toward_base()
    {
        var definition = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "threat.passive",
            Name = "Threat Passive",
            Kind = AbilitySpecKind.Passive,
            ThreatValue = 50
        });
        var friendly = Combatant("friendly", CombatTeam.Friendly, [definition], canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, [], canBasicAttack: false);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(150f, friendly.Threat);
        var stats = Assert.Single(result.EntityStats, item => item.EntityId == friendly.Id);
        Assert.Equal(50, stats.ThreatGenerated);
        Assert.Equal(50, Assert.Single(stats.Abilities).TotalThreat);
        var decayPerTick = 1d - Math.Pow(0.5d, 1d / 150d);
        Assert.Equal(125f, friendly.GetThreat(150, decayPerTick), 2);
        friendly.AdjustThreat(-1_000);
        Assert.Equal(0f, friendly.Threat);
    }

    [Fact]
    public void Basic_attack_threat_is_reported_in_entity_and_ability_stats()
    {
        var friendly = Combatant("friendly", CombatTeam.Friendly, []);
        var hostile = Combatant("hostile", CombatTeam.Hostile, [], canBasicAttack: false);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1,
                ThreatHalfLifeSeconds: 0,
                BasicAttackThreatValue: 8));

        var result = engine.Run([friendly], [hostile]);

        var stats = Assert.Single(result.EntityStats, item => item.EntityId == friendly.Id);
        Assert.Equal(8, stats.ThreatGenerated);
        Assert.Equal(8, Assert.Single(stats.Abilities, ability => ability.Name == "Basic Attack").TotalThreat);
    }

    [Fact]
    public void Threat_attribute_sets_and_updates_runtime_baseline()
    {
        var combatant = new RuntimeCombatant(
            "tank",
            "Tank",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Threat] = 150
            },
            []);

        Assert.Equal(150, combatant.GetAttribute(AttributeType.Threat));
        Assert.Equal(150, combatant.Threat);

        combatant.AdjustAttribute(AttributeType.Threat, 25);

        Assert.Equal(175, combatant.GetAttribute(AttributeType.Threat));
        Assert.Equal(175, combatant.Threat);
    }

    [Fact]
    public void Feature_flag_restores_legacy_threat_generation_behavior()
    {
        var definition = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "disabled-threat.passive",
            Name = "Disabled Threat Passive",
            Kind = AbilitySpecKind.Passive,
            ThreatValue = 500
        });
        var friendly = Combatant("friendly", CombatTeam.Friendly, [definition], canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, [], canBasicAttack: false);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1_000,
                ThreatAndTankingEnabled: false));

        engine.Run([friendly], [hostile]);

        Assert.Equal(RuntimeCombatant.BaseThreat, friendly.Threat);
    }

    [Fact]
    public void Hard_taunt_forces_every_single_target_attack_to_the_taunter()
    {
        var taunter = Combatant("taunter", CombatTeam.Friendly, [], canBasicAttack: false);
        var ally = Combatant("ally", CombatTeam.Friendly, [], canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, []);
        taunter.Conditions.Add(new RuntimeCondition(
            StandardConditionType.Taunt,
            taunter,
            taunter,
            1,
            0,
            0,
            1,
            "Taunt"));
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 100, BasicAttackIntervalTicks: 1));

        var result = engine.Run([taunter, ally], [hostile]);

        Assert.True(result.EntityStats.Single(stats => stats.EntityId == taunter.Id).TargetedAttacks > 0);
        Assert.DoesNotContain(
            result.EntityStats,
            stats => stats.EntityId == ally.Id && stats.TargetedAttacks > 0);
    }

    [Theory]
    [InlineData(AbilityTargetSelector.CurrentTarget)]
    [InlineData(AbilityTargetSelector.RandomEnemy)]
    [InlineData(AbilityTargetSelector.LowestHealthEnemy)]
    [InlineData(AbilityTargetSelector.HighestHealthEnemy)]
    [InlineData(AbilityTargetSelector.LowestCurrentHealthEnemy)]
    [InlineData(AbilityTargetSelector.HighestMaxHealthEnemy)]
    public void Hard_taunt_overrides_specialized_single_enemy_selectors(AbilityTargetSelector selector)
    {
        var attack = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = $"attack.{selector}",
            Name = "Attack",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            ThreatValue = 0,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = selector,
                    BaseValue = 100,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        });
        var taunter = Combatant("taunter", CombatTeam.Friendly, [], canBasicAttack: false);
        var ally = Combatant("ally", CombatTeam.Friendly, [], canBasicAttack: false);
        ally.AdjustHealth(-500_000);
        taunter.Conditions.Add(new RuntimeCondition(
            StandardConditionType.Taunt,
            taunter,
            taunter,
            1,
            0,
            0,
            1,
            "Taunt"));
        var hostile = Combatant("hostile", CombatTeam.Hostile, [attack], canBasicAttack: false);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([taunter, ally], [hostile]);

        Assert.Equal(1, result.EntityStats.Single(stats => stats.EntityId == taunter.Id).TargetedAttacks);
        Assert.DoesNotContain(
            result.EntityStats,
            stats => stats.EntityId == ally.Id && stats.TargetedAttacks > 0);
    }

    [Fact]
    public void Attention_curve_turns_three_times_threat_into_about_eighty_percent_attention()
    {
        var tank = Combatant("tank", CombatTeam.Friendly, [], canBasicAttack: false);
        tank.AdjustThreat(200);
        var allies = Enumerable.Range(1, 4)
            .Select(index => Combatant($"ally-{index}", CombatTeam.Friendly, [], canBasicAttack: false))
            .ToArray();
        var hostile = Combatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 2_000,
                BasicAttackIntervalTicks: 1,
                ThreatHalfLifeSeconds: 0,
                BasicAttackThreatValue: 0,
                RandomSeed: 73));

        var result = engine.Run([tank, .. allies], [hostile]);
        var share = result.EntityStats.Single(stats => stats.EntityId == tank.Id).AttentionSharePercent;

        Assert.InRange(share, 75d, 85d);
    }

    [Fact]
    public void Cover_redirects_budgeted_damage_through_the_guardian()
    {
        var coverAbility = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "cover",
            Name = "Cover",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            ThreatValue = 0,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "cover.effect",
                    Operation = AbilityEffectOperation.GrantCover,
                    Target = AbilityTargetSelector.AllAllies,
                    BaseValue = 50,
                    DurationTicks = 10
                }
            ]
        });
        var guardian = Combatant("guardian", CombatTeam.Friendly, [coverAbility], canBasicAttack: false);
        var ally = Combatant("ally", CombatTeam.Friendly, [], canBasicAttack: false);
        ally.Conditions.Add(new RuntimeCondition(
            StandardConditionType.Taunt,
            ally,
            ally,
            1,
            0,
            0,
            1,
            "Taunt"));
        var hostile = Combatant("hostile", CombatTeam.Hostile, [], power: 100);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1, RandomSeed: 17));

        var result = engine.Run([guardian, ally], [hostile]);
        var guardianStats = result.EntityStats.Single(stats => stats.EntityId == guardian.Id);
        var allyStats = result.EntityStats.Single(stats => stats.EntityId == ally.Id);

        Assert.True(guardianStats.DamageRedirectedTo > 0);
        Assert.Equal(guardianStats.DamageRedirectedTo, allyStats.DamageRedirectedAway);
        Assert.True(guardianStats.DamageTaken > 0);
        Assert.True(allyStats.DamageTaken > 0);
    }

    [Fact]
    public void Summons_default_to_one_quarter_threat_weight()
    {
        var compiled = AbilityCompiler.CompileSummon(new SummonSpec
        {
            Id = "summon",
            Name = "Summon",
            Attributes =
            [
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.MaxHealth,
                    BaseValue = 10,
                    MinimumValue = 1
                }
            ]
        });

        Assert.Equal(0.25f, compiled.ThreatMultiplier);
    }

    private static RuntimeCombatant Combatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        bool canBasicAttack = true,
        float power = 0) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000_000,
                [AttributeType.Power] = power
            },
            abilities,
            canBasicAttack: canBasicAttack,
            basicAttackType: AttackType.Melee,
            basicAttackDamageType: DamageType.Physical);
}
