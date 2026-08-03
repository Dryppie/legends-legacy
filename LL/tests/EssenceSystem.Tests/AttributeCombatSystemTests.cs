using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class AttributeCombatSystemTests
{
    [Fact]
    public void Attribute_catalog_defines_canonical_metadata_for_every_attribute()
    {
        var attributeTypes = Enum.GetValues<AttributeType>();
        var definitions = AttributeCatalog.All;

        Assert.Equal(attributeTypes.Length, definitions.Count);
        Assert.Equal(
            attributeTypes.Order(),
            definitions.Select(x => x.AttributeType).Order());
        Assert.All(definitions, definition =>
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(definition.Description));
            Assert.True(definition.MinimumValue >= 0);
            Assert.InRange(definition.DisplayPrecision, 0, 4);
            Assert.True(definition.IsEquipmentEligible);
            Assert.True(definition.IsContentFacing);
            Assert.NotEmpty(definition.RelevantBenchmarkScenarios);
            Assert.Equal(
                definition.Unit == AttributeUnit.PercentagePoints,
                definition.DisplaySuffix == "%");
            Assert.Equal(
                definition.Unit == AttributeUnit.PercentagePoints ? 2 : 0,
                definition.DisplayPrecision);
        });

        var healthRegeneration = AttributeCatalog.Get(AttributeType.HealthRegeneration);
        Assert.Equal(AttributeUnit.HealthPerFiveSeconds, healthRegeneration.Unit);
        Assert.Equal(" HP/5s", healthRegeneration.DisplaySuffix);

        Assert.Equal(
            EquipmentStatBudgetCatalog.Attributes.Order(),
            definitions
                .Where(x => x.IsEquipmentEligible)
                .Select(x => x.AttributeType)
                .Order());
    }

    [Fact]
    public void Attribute_catalog_caps_and_primary_sources_match_combat_rules()
    {
        var fixedCaps = new Dictionary<AttributeType, float>
        {
            [AttributeType.CritChance] = AttributeCombatRules.CritChanceCapPercent,
            [AttributeType.DodgeChance] = AttributeCombatRules.DodgeChanceCapPercent,
            [AttributeType.BlockChance] = AttributeCombatRules.BlockChanceCapPercent,
            [AttributeType.DamageReduction] = AttributeCombatRules.DamageReductionCapPercent,
            [AttributeType.LifeSteal] = AttributeCombatRules.LifeStealCapPercent,
            [AttributeType.Cooldown] = AttributeCombatRules.CooldownReductionCapPercent
        };

        foreach (var (attribute, expectedCap) in fixedCaps)
        {
            Assert.Equal(AttributeCapKind.Fixed, AttributeCatalog.Get(attribute).CapKind);
            Assert.Equal(expectedCap, AttributeCatalog.GetFixedCap(attribute));
            Assert.True(AttributeCatalog.TryGetEffectiveCharacterCap(attribute, 1, out var effectiveCap));
            Assert.Equal(expectedCap, effectiveCap);
        }

        Assert.Equal(
            AttributeCapKind.ContextDependent,
            AttributeCatalog.Get(AttributeType.AttackSpeed).CapKind);
        Assert.True(AttributeCatalog.TryGetEffectiveCharacterCap(
            AttributeType.AttackSpeed,
            0.75,
            out var fastWeaponCap));
        Assert.True(AttributeCatalog.TryGetEffectiveCharacterCap(
            AttributeType.AttackSpeed,
            1.25,
            out var slowWeaponCap));
        Assert.True(slowWeaponCap > fastWeaponCap);

        foreach (var contribution in AttributeCombatRules.PrimaryContributions)
        {
            Assert.Equal(
                contribution.PrimaryAttribute,
                AttributeCatalog.Get(contribution.DerivedAttribute).ApprovedPrimarySource);
        }

        Assert.DoesNotContain(
            AttributeCombatRules.PrimaryContributions,
            contribution => AttributeCombatRules.IsPrimary(contribution.DerivedAttribute));
    }

    [Fact]
    public void Primary_attributes_project_only_into_their_approved_groups()
    {
        var projected = AttributeCalculator.CalculateProjectedAttributes(
            new Dictionary<AttributeType, float>
            {
                [AttributeType.Power] = 10,
                [AttributeType.Fortitude] = 10,
                [AttributeType.Precision] = 10,
                [AttributeType.Spirit] = 10,
                [AttributeType.MaxHealth] = 100
            },
            Array.Empty<AttributeModifierBase>());

        Assert.Equal(10, projected[AttributeType.Power]);
        Assert.Equal(140, projected[AttributeType.MaxHealth]);
        Assert.Equal(5, projected[AttributeType.Armor], 3);
        Assert.Equal(5, projected[AttributeType.Resistance], 3);
        Assert.Equal(1, projected[AttributeType.CritChance], 3);
        Assert.Equal(1, projected[AttributeType.ArmorPenetration], 3);
        Assert.Equal(1, projected[AttributeType.MagicPenetration], 3);
        Assert.Equal(0.5f, projected[AttributeType.AttackSpeed], 3);
        Assert.Equal(1.5f, projected[AttributeType.HealingPowerPercent], 3);
        Assert.Equal(0.5f, projected[AttributeType.HealthRegeneration], 3);
        Assert.Equal(1, projected[AttributeType.StatusResistance], 3);
        Assert.Equal(1, projected[AttributeType.CrowdControlResistance], 3);
        Assert.Equal(0.5f, projected[AttributeType.SummonPower], 3);
        Assert.Equal(1, projected[AttributeType.SummonHealth], 3);
        Assert.Equal(0, projected.GetValueOrDefault(AttributeType.Cooldown));
        Assert.Equal(0, projected.GetValueOrDefault(AttributeType.DamageReduction));
    }

    [Fact]
    public void Runtime_primary_changes_update_only_their_dependency_group()
    {
        var attributes = AttributeCalculator.CalculateProjectedAttributes(
            new Dictionary<AttributeType, float>
            {
                [AttributeType.Power] = 10,
                [AttributeType.Fortitude] = 10,
                [AttributeType.Precision] = 10,
                [AttributeType.Spirit] = 10,
                [AttributeType.MaxHealth] = 100
            },
            Array.Empty<AttributeModifierBase>());
        var combatant = CreateCombatant("primary", CombatTeam.Friendly, [], attributes);

        combatant.AdjustAttribute(AttributeType.Fortitude, 10);
        combatant.AdjustAttribute(AttributeType.Spirit, 10);

        Assert.Equal(180, combatant.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(180, combatant.Health);
        Assert.Equal(10, combatant.GetAttribute(AttributeType.Armor), 3);
        Assert.Equal(10, combatant.GetAttribute(AttributeType.Resistance), 3);
        Assert.Equal(3, combatant.GetAttribute(AttributeType.HealingPowerPercent), 3);
        Assert.Equal(1, combatant.GetAttribute(AttributeType.HealthRegeneration), 3);
        Assert.Equal(10, combatant.GetAttribute(AttributeType.Power));
        Assert.Equal(0, combatant.GetAttribute(AttributeType.Cooldown));
    }

    [Fact]
    public void Catalog_rejects_non_power_ability_effect_scaling()
    {
        var ability = CreateEffectAbility(
            "invalid-scaling",
            new AbilityEffectSpec
            {
                Id = "effect.invalid",
                Operation = AbilityEffectOperation.Heal,
                Target = AbilityTargetSelector.Self,
                BaseValue = 10,
                ScalingAttribute = AttributeType.Spirit,
                ScalingCoefficient = 1
            });

        var validation = AbilityCatalogValidator.Validate([ability], []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("scale only with Power", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(CritEligibility.Default, true)]
    [InlineData(CritEligibility.Allowed, true)]
    [InlineData(CritEligibility.Disallowed, false)]
    public void Direct_healing_crits_by_default_and_supports_explicit_override(
        CritEligibility eligibility,
        bool shouldCrit)
    {
        var ability = CreateEffectAbility(
            $"healing-{eligibility}",
            new AbilityEffectSpec
            {
                Id = "effect.heal",
                Operation = AbilityEffectOperation.Heal,
                Target = AbilityTargetSelector.Self,
                BaseValue = 10,
                CritEligibility = eligibility
            });
        var actor = CreateCombatant(
            "healer",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.CritChance] = 75,
                [AttributeType.CritDamage] = 100
            });
        actor.SetHealth(100);
        var target = CreateCombatant("target", CombatTeam.Hostile, []);

        var result = RunSingleTick(actor, target, randomSeed: 1);
        var healing = Assert.Single(
            result.EventLog,
            log =>
                log.Source == "effect.heal"
                && log.EventType is EventType.Heal or EventType.HealCrit);

        Assert.Equal(shouldCrit ? EventType.HealCrit : EventType.Heal, healing.EventType);
        Assert.Equal(shouldCrit ? 20 : 10, healing.Magnitude);
    }

    [Fact]
    public void Healing_uses_power_then_healing_power()
    {
        var ability = CreateEffectAbility(
            "power-healing",
            new AbilityEffectSpec
            {
                Id = "effect.heal",
                Operation = AbilityEffectOperation.Heal,
                Target = AbilityTargetSelector.Self,
                BaseValue = 10,
                ScalingAttribute = AttributeType.Power,
                ScalingCoefficient = 0.2f,
                CritEligibility = CritEligibility.Disallowed
            });
        var actor = CreateCombatant(
            "healer",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.Power] = 50,
                [AttributeType.HealingPowerPercent] = 50
            });
        actor.SetHealth(100);
        var target = CreateCombatant("target", CombatTeam.Hostile, []);

        var result = RunSingleTick(actor, target);

        var healing = Assert.Single(
            result.EventLog,
            log => log.Source == "effect.heal"
                   && log.EventType == EventType.Heal);
        Assert.InRange(healing.Magnitude, 24, 36);
    }

    [Fact]
    public void Periodic_damage_does_not_crit_without_an_explicit_opt_in()
    {
        var ability = CreateEffectAbility(
            "dot",
            new AbilityEffectSpec
            {
                Id = "effect.dot",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = 10,
                DurationTicks = 2,
                IntervalTicks = 1,
                AttackType = AttackType.DamageOverTime,
                DamageType = DamageType.Poison
            });
        var actor = CreateCombatant(
            "source",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.CritChance] = 75,
                [AttributeType.CritDamage] = 100
            });
        var target = CreateCombatant("target", CombatTeam.Hostile, []);

        var result = RunSingleTick(actor, target, randomSeed: 1);

        Assert.Contains(result.EventLog, log => log.Source == "effect.dot" && log.EventType == EventType.Damage);
        Assert.DoesNotContain(result.EventLog, log => log.Source == "effect.dot" && log.EventType == EventType.DamageCrit);
    }

    [Fact]
    public void Armor_and_penetration_use_the_same_typed_defense_scale()
    {
        var ability = CreateDamageAbility(100, DamageType.Physical);
        var compiled = AbilityCompiler.CompileAbilities([ability]).Values;
        var armoredTarget = CreateCombatant(
            "armored",
            CombatTeam.Hostile,
            [],
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Armor] = 100
            });
        var source = CreateCombatant("source", CombatTeam.Friendly, compiled);

        RunSingleTick(source, armoredTarget);

        Assert.Equal(950, armoredTarget.Health);

        var penetratedTarget = CreateCombatant(
            "penetrated",
            CombatTeam.Hostile,
            [],
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Armor] = 100
            });
        var penetratingSource = CreateCombatant(
            "penetrating-source",
            CombatTeam.Friendly,
            compiled,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.ArmorPenetration] = 100
            });

        RunSingleTick(penetratingSource, penetratedTarget);

        Assert.Equal(900, penetratedTarget.Health);
    }

    [Fact]
    public void Damage_prevention_telemetry_is_non_overlapping_and_reconciles_each_hit()
    {
        var source = CreateCombatant(
            "source",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([CreateDamageAbility(100, DamageType.Physical)]).Values);
        var target = CreateCombatant(
            "target",
            CombatTeam.Hostile,
            [],
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Armor] = 100,
                [AttributeType.DamageReduction] = 20
            });
        target.AdjustBarrier(10);

        var result = RunSingleTick(source, target);
        var damage = Assert.Single(result.EventLog, x =>
            x.ActorId == source.Id
            && x.EventType == EventType.Damage);
        var stats = result.EntityStats.Single(x => x.EntityId == target.Id);

        Assert.Equal(100, damage.IncomingRawDamage);
        Assert.Equal(50, damage.TypedMitigationPrevented);
        Assert.Equal(50, damage.PhysicalMitigationPrevented);
        Assert.Equal(0, damage.MagicalMitigationPrevented);
        Assert.Equal(0, damage.BlockPrevented);
        Assert.Equal(10, damage.DamageReductionPrevented);
        Assert.Equal(0, damage.DamageAmplified);
        Assert.Equal(10, damage.BarrierAbsorbed);
        Assert.Equal(30, damage.FinalHealthDamage);
        Assert.True(damage.PreventionTelemetryReconciles);

        Assert.Equal(100, stats.IncomingRawDamage);
        Assert.Equal(50, stats.TypedMitigationPrevented);
        Assert.Equal(50, stats.PhysicalMitigationPrevented);
        Assert.Equal(0, stats.MagicalMitigationPrevented);
        Assert.Equal(10, stats.DamageReductionPrevented);
        Assert.Equal(10, stats.DamageBlocked);
        Assert.Equal(30, stats.FinalHealthDamage);
        Assert.Equal(30, stats.DamageTaken);
        Assert.True(stats.PreventionTelemetryReconciles);
    }

    [Fact]
    public void Dodge_telemetry_records_the_avoided_incoming_hit_without_other_prevention()
    {
        CombatResult? dodgedResult = null;
        for (var seed = 1; seed <= 100 && dodgedResult is null; seed++)
        {
            var ability = CreateEffectAbility(
                "dodgeable",
                new AbilityEffectSpec
                {
                    Id = "effect.dodgeable",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical,
                    CritEligibility = CritEligibility.Disallowed
                });
            var source = CreateCombatant(
                $"source-{seed}",
                CombatTeam.Friendly,
                AbilityCompiler.CompileAbilities([ability]).Values);
            var target = CreateCombatant(
                $"target-{seed}",
                CombatTeam.Hostile,
                [],
                new Dictionary<AttributeType, float>
                {
                    [AttributeType.MaxHealth] = 1_000,
                    [AttributeType.DodgeChance] = 50
                });
            var result = RunSingleTick(source, target, seed);
            if (result.EventLog.Any(x => x.EventType == EventType.Miss))
                dodgedResult = result;
        }

        var dodged = Assert.IsType<CombatResult>(dodgedResult);
        var miss = Assert.Single(dodged.EventLog, x => x.EventType == EventType.Miss);
        var stats = dodged.EntityStats.Single(x => x.EntityId == miss.TargetId);

        Assert.Equal(100, miss.IncomingRawDamage);
        Assert.Equal(100, miss.AvoidedDamage);
        Assert.Equal(1, stats.AvoidedAttacks);
        Assert.Equal(100, stats.AvoidedDamage);
        Assert.Equal(0, stats.FinalHealthDamage);
        Assert.Equal(0, stats.DamageAmplified);
        Assert.True(stats.PreventionTelemetryReconciles);
    }

    [Fact]
    public void Character_life_steal_uses_post_mitigation_health_damage_and_does_not_crit()
    {
        var ability = CreateDamageAbility(40, DamageType.Physical);
        var actor = CreateCombatant(
            "drainer",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.LifeSteal] = 50,
                [AttributeType.CritChance] = 75,
                [AttributeType.CritDamage] = 100
            });
        actor.SetHealth(100);
        var target = CreateCombatant("target", CombatTeam.Hostile, []);

        var result = RunSingleTick(actor, target, randomSeed: 1);

        Assert.Equal(120, actor.Health);
        Assert.Contains(result.EventLog, log => log.ActorId == actor.Id && log.EventType == EventType.Heal && log.Magnitude == 20);
        Assert.DoesNotContain(result.EventLog, log => log.ActorId == actor.Id && log.EventType == EventType.HealCrit);
    }

    [Fact]
    public void Cooldown_reduction_is_globally_capped_at_forty_percent()
    {
        var definition = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "cooldown",
            Name = "Cooldown",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100
        });
        var baseline = new RuntimeAbility(definition);
        var capped = new RuntimeAbility(definition);
        var overcapped = new RuntimeAbility(definition);

        baseline.StartCooldown(0);
        capped.StartCooldown(40);
        overcapped.StartCooldown(400);

        Assert.Equal(100, baseline.RemainingCooldownTicks);
        Assert.Equal(60, capped.RemainingCooldownTicks);
        Assert.Equal(60, overcapped.RemainingCooldownTicks);
        Assert.Equal(
            AttributeCombatRules.CooldownReductionCapPercent,
            EquipmentStatBudgetCatalog.Get(AttributeType.Cooldown, tier: 1).HardCap);
        Assert.Equal(
            AttributeCombatRules.CooldownReductionCapPercent,
            EquipmentStatBudgetCatalog.Get(AttributeType.Cooldown, tier: 1).PerItemHardCap);
    }

    [Theory]
    [InlineData(0.75d, 200f)]
    [InlineData(1d, 300f)]
    [InlineData(1.25d, 400f)]
    public void Useful_attack_speed_cap_depends_on_weapon_interval(
        double intervalMultiplier,
        float expectedCap)
    {
        Assert.True(AttributeCombatRules.TryGetEffectiveCharacterCap(
            AttributeType.AttackSpeed,
            intervalMultiplier,
            out var cap));
        Assert.Equal(expectedCap, cap);
        Assert.Equal(
            AttributeCombatRules.MaximumBasicAttackRate,
            AttributeCombatRules.CalculateBasicAttackRate(cap, intervalMultiplier));
        Assert.Equal(
            AttributeCombatRules.MaximumBasicAttackRate,
            AttributeCombatRules.CalculateBasicAttackRate(cap + 1_000, intervalMultiplier));
    }

    [Fact]
    public void Equipment_budget_evaluator_uses_the_same_catalog_as_generation()
    {
        AttributeModifierBase[] modifiers =
        [
            new InstanceAttributeModifier(AttributeType.Power, 2),
            new InstanceAttributeModifier(AttributeType.MaxHealth, 10),
            new InstanceAttributeModifier(AttributeType.Armor, 5)
        ];

        var total = EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 1);
        var breakdown = EquipmentBudgetEvaluator.EvaluateByAttribute(modifiers, tier: 1);

        Assert.Equal(7.2d, total);
        Assert.Equal(2.5d, breakdown[AttributeType.Power]);
        Assert.Equal(2d, breakdown[AttributeType.MaxHealth]);
        Assert.Equal(2.7d, breakdown[AttributeType.Armor]);
    }

    [Theory]
    [InlineData(1, 10d)]
    [InlineData(5, 39d)]
    [InlineData(10, 152d)]
    public void Equal_budget_armor_and_health_have_similar_marginal_effective_health(
        int tier,
        double budget)
    {
        var primary = 8f * tier;
        var baselineHealth = 180 + tier * 80 + primary * 4;
        var baselineArmor = tier * 5 + primary * 0.5f;
        var healthPoints =
            (float)(budget / EquipmentStatBudgetCatalog.Get(AttributeType.MaxHealth, tier).CostPerPoint);
        var armorPoints =
            (float)(budget / EquipmentStatBudgetCatalog.Get(AttributeType.Armor, tier).CostPerPoint);
        var baselineEffectiveHealth =
            AttributeCombatRules.CalculateEffectiveHealth(baselineHealth, baselineArmor);
        var healthEffectiveHealth =
            AttributeCombatRules.CalculateEffectiveHealth(baselineHealth + healthPoints, baselineArmor);
        var armorEffectiveHealth =
            AttributeCombatRules.CalculateEffectiveHealth(baselineHealth, baselineArmor + armorPoints);
        var healthGain = healthEffectiveHealth - baselineEffectiveHealth;
        var armorGain = armorEffectiveHealth - baselineEffectiveHealth;
        var relativeDifference =
            Math.Abs(armorGain - healthGain) / healthGain;

        Assert.InRange(relativeDifference, 0, 0.1f);
    }

    [Fact]
    public void Status_resistance_shortens_status_duration()
    {
        var status = new StatusSpec
        {
            Id = "status.test",
            Name = "Test",
            DurationTicks = 100,
            MaxStacks = 1,
            Tags = ["Ailment"]
        };
        var ability = CreateEffectAbility(
            "apply-status",
            new AbilityEffectSpec
            {
                Id = "effect.status",
                Operation = AbilityEffectOperation.ApplyStatus,
                Target = AbilityTargetSelector.CurrentTarget,
                StatusId = status.Id,
                BaseValue = 1
            });
        var source = CreateCombatant(
            "source",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values);
        var target = CreateCombatant(
            "target",
            CombatTeam.Hostile,
            [],
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.StatusResistance] = 100
            });
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses([status]),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        engine.Run([source], [target]);

        Assert.Equal(49, Assert.Single(target.Statuses).RemainingDurationTicks);
    }

    [Fact]
    public void Status_resistance_also_shortens_timed_effects_owned_by_the_status()
    {
        var status = new StatusSpec
        {
            Id = "status.timed-debuff",
            Name = "Timed Debuff",
            DurationTicks = 100,
            MaxStacks = 1,
            Tags = ["Ailment"],
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["effect.timed-debuff"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.timed-debuff",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.Power,
                    BaseValue = -10,
                    DurationTicks = 100
                }
            ]
        };
        var ability = CreateEffectAbility(
            "apply-timed-debuff",
            new AbilityEffectSpec
            {
                Id = "effect.status",
                Operation = AbilityEffectOperation.ApplyStatus,
                Target = AbilityTargetSelector.CurrentTarget,
                StatusId = status.Id,
                BaseValue = 1
            });
        var source = CreateCombatant(
            "source",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([ability]).Values);
        var target = CreateCombatant(
            "target",
            CombatTeam.Hostile,
            [],
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.Power] = 20,
                [AttributeType.StatusResistance] = 100
            });
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses([status]),
            new FastCombatEngineOptions(MaxTicks: 60, BasicAttackIntervalTicks: 1_000));

        engine.Run([source], [target]);

        Assert.Equal(20, target.GetAttribute(AttributeType.Power));
        Assert.Empty(target.ActiveEffects);
    }

    private static CombatResult RunSingleTick(
        RuntimeCombatant friendly,
        RuntimeCombatant hostile,
        int randomSeed = 1337)
    {
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1_000,
                RandomSeed: randomSeed));
        return engine.Run([friendly], [hostile]);
    }

    private static AbilitySpec CreateDamageAbility(int baseValue, DamageType damageType) =>
        CreateEffectAbility(
            "damage",
            new AbilityEffectSpec
            {
                Id = "effect.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = baseValue,
                AttackType = AttackType.None,
                DamageType = damageType,
                CritEligibility = CritEligibility.Disallowed
            });

    private static AbilitySpec CreateEffectAbility(string id, AbilityEffectSpec effect) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects = [effect]
        };

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        IReadOnlyDictionary<AttributeType, float>? attributes = null)
    {
        var resolved = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 200,
            [AttributeType.Power] = 0,
            [AttributeType.CritDamage] = 100,
            [AttributeType.AttackSpeed] = 0
        };

        if (attributes is not null)
        {
            foreach (var (attribute, value) in attributes)
                resolved[attribute] = value;
        }

        return new RuntimeCombatant(id, id, team, resolved, abilities);
    }
}
