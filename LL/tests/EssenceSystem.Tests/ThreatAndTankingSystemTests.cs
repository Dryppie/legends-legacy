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
    public void Maintained_conditional_modifier_uses_sustained_threat_instead_of_trigger_threat()
    {
        var ability = MaintainedPhysicalDefenseAbility();

        Assert.Equal(0, AbilityThreatRules.GetThreatValue(ability));
        Assert.Equal(5d, AbilityThreatRules.GetEstimatedThreatPerSecond(ability), precision: 5);
        Assert.True(AbilityThreatRules.HasMaintainedThreat(ability));

        var compiledEffect = AbilityCompiler.CompileAbility(ability)
            .TriggersByEvent[AbilityTriggerEvent.OnCombatStart]
            .Single()
            .Effects
            .Single();
        Assert.Equal(AbilityThreatFunctionBand.ProtectiveSelf, compiledEffect.MaintainedThreatBand);
        Assert.Equal(5f, compiledEffect.MaintainedThreatPerSecond);
    }

    [Fact]
    public void Maintained_conditional_modifier_applies_once_and_generates_exact_sustained_threat()
    {
        var definition = AbilityCompiler.CompileAbility(MaintainedPhysicalDefenseAbility());
        var friendly = Combatant("friendly", CombatTeam.Friendly, [definition], canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, [], canBasicAttack: false);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 20,
                BasicAttackIntervalTicks: 1_000,
                ThreatHalfLifeSeconds: 0));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(-10f, friendly.GetDamageTakenPercent(DamageType.Physical, hostile));
        var stats = result.EntityStats.Single(item => item.EntityId == friendly.Id);
        var abilityStats = Assert.Single(stats.Abilities, item => item.Name == definition.Name);
        Assert.Equal(1, abilityStats.Uses);
        Assert.Equal(10, abilityStats.TotalThreat);
    }

    [Fact]
    public void Maintained_conditional_modifier_is_removed_when_health_crosses_its_threshold()
    {
        var defense = AbilityCompiler.CompileAbility(MaintainedPhysicalDefenseAbility());
        var attack = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "threshold.attack",
            Name = "Threshold Attack",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            ThreatValue = 0,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "threshold.attack.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.RandomEnemy,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        });
        var friendly = Combatant("friendly", CombatTeam.Friendly, [defense], canBasicAttack: false);
        var hostile = Combatant("hostile", CombatTeam.Hostile, [attack], canBasicAttack: false, power: 700_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([friendly], [hostile]);

        Assert.True(friendly.IsAlive);
        Assert.True(friendly.Health < 500_000);
        Assert.Equal(0f, friendly.GetDamageTakenPercent(DamageType.Physical, hostile));
        var stats = result.EntityStats.Single(item => item.EntityId == friendly.Id);
        Assert.Equal(1, Assert.Single(stats.Abilities, item => item.Name == defense.Name).Uses);
    }

    [Fact]
    public void One_shot_passive_threat_uses_its_activation_delay_instead_of_its_lockout()
    {
        var oneShot = new AbilitySpec
        {
            Kind = AbilitySpecKind.Passive,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnInterval,
                    InternalCooldownTicks = 100_000,
                    InitialDelayTicks = 150,
                    EffectIds = ["recovery"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "recovery",
                    Operation = AbilityEffectOperation.ApplyCondition,
                    Target = AbilityTargetSelector.AllAllies,
                    Condition = StandardConditionType.Recovery,
                    Uses = 1
                }
            ]
        };
        var repeating = new AbilitySpec
        {
            Kind = AbilitySpecKind.Passive,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnInterval,
                    InternalCooldownTicks = 40,
                    InitialDelayTicks = 40,
                    EffectIds = ["barrier"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.AllAllies,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f
                }
            ]
        };

        Assert.Equal(75, AbilityThreatRules.GetThreatValue(oneShot));
        Assert.Equal(150, AbilityThreatRules.GetTriggerPeriodTicks(oneShot, Assert.Single(oneShot.Triggers)));
        Assert.Equal(20, AbilityThreatRules.GetThreatValue(repeating));
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

    [Theory]
    [InlineData(AbilityTargetSelector.RandomEnemy)]
    [InlineData(AbilityTargetSelector.TwoRandomEnemies)]
    public void Random_enemy_selectors_ignore_hard_taunt(AbilityTargetSelector selector)
    {
        var attack = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = $"attack.{selector}",
            Name = "Random Attack",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 1,
            ThreatValue = 0,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = selector,
                    BaseValue = 1,
                    AttackType = AttackType.Ranged,
                    DamageType = DamageType.Magical,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        });
        var taunter = Combatant("taunter", CombatTeam.Friendly, [], canBasicAttack: false);
        var ally = Combatant("ally", CombatTeam.Friendly, [], canBasicAttack: false);
        var secondAlly = Combatant("second-ally", CombatTeam.Friendly, [], canBasicAttack: false);
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
            new FastCombatEngineOptions(
                MaxTicks: 50,
                BasicAttackIntervalTicks: 1_000,
                RandomSeed: 73));

        var result = engine.Run([taunter, ally, secondAlly], [hostile]);

        Assert.True(result.EntityStats.Single(stats => stats.EntityId == ally.Id).TargetedAttacks > 0);
        Assert.True(result.EntityStats.Single(stats => stats.EntityId == secondAlly.Id).TargetedAttacks > 0);
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

    private static AbilitySpec MaintainedPhysicalDefenseAbility() => new()
    {
        Id = "conditional-defense",
        Name = "Conditional Defense",
        Kind = AbilitySpecKind.Passive,
        Triggers =
        [
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnCombatStart,
                EffectIds = ["conditional-defense.effect"]
            },
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnHealthChanged,
                EffectIds = ["conditional-defense.effect"]
            }
        ],
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = "conditional-defense.effect",
                Operation = AbilityEffectOperation.ModifyDamageTaken,
                Target = AbilityTargetSelector.Self,
                BaseValue = -10,
                DamageType = DamageType.Physical,
                MaintainWhileConditionsMet = true,
                Conditions =
                [
                    new AbilityConditionSpec
                    {
                        Type = AbilityConditionType.HealthAbovePercent,
                        Subject = AbilityConditionSubject.Source,
                        Value = 50
                    }
                ]
            }
        ]
    };
}
