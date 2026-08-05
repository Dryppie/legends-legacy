using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.Interfaces.Combat.Resolution;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AbilitySystemTests
{
    [Fact]
    public void Catalog_indexes_500_authored_abilities_without_scanning_runtime_combat()
    {
        var abilities = Enumerable.Range(0, 500)
            .Select(index => CreateDamageAbility($"ability.scale.{index}", index % 2 == 0 ? "Family.Fire" : "Family.Ice"))
            .ToList();
        var owners = abilities.ToDictionary(x => x.Id, x => $"essence.{x.Id}", StringComparer.OrdinalIgnoreCase);

        var catalog = AbilityCatalogValidator.CreateCatalog(abilities, [CreateBurnStatus()], owners);

        Assert.Equal(500, catalog.AbilitiesById.Count);
        Assert.Equal(500, catalog.AbilityIdsByKind[AbilitySpecKind.Active].Count);
        Assert.Equal(250, catalog.AbilityIdsByTag["Family.Fire"].Count);
        Assert.Equal(500, catalog.AbilityIdsByTrigger[AbilityTriggerEvent.OnAbilityUsed].Count);
        Assert.Equal("essence.ability.scale.42", catalog.OwningEssenceByAbilityId["ability.scale.42"]);
        Assert.Equal(["ability.scale.42"], catalog.AbilityIdsByOwningEssence["essence.ability.scale.42"]);
    }

    [Fact]
    public void Catalog_reports_grouped_validation_failures()
    {
        var invalid = CreateDamageAbility("ability.invalid", "Family.Test");
        invalid.Effects[0].Operation = AbilityEffectOperation.ApplyStatus;
        invalid.Effects[0].StatusId = "missing.status";

        var validation = AbilityCatalogValidator.Validate([invalid], []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Contains("ability.invalid/effect.damage", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, x => x.Contains("missing.status", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_validates_summon_effect_references()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.missing",
            Kind = AbilitySpecKind.Active,
            Name = "Missing Summon",
            Effects =
            [
                new()
                {
                    Id = "effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "missing.summon"
                }
            ]
        };

        var validation = AbilityCatalogValidator.Validate([summonAbility], [], summons: []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Contains("ability.summon.missing/effect.summon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, x => x.Contains("missing.summon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Engine_executes_direct_damage_and_barrier()
    {
        var strike = CreateDamageAbility("ability.strike", "Family.Test");
        var barrier = new AbilitySpec
        {
            Id = "ability.barrier",
            Kind = AbilitySpecKind.Active,
            Name = "Barrier",
            Effects =
            [
                new()
                {
                    Id = "effect.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 15
                }
            ]
        };

        var result = RunBattle([strike, barrier], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(15, friendly.Barrier);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.Damage && x.Source == "effect.damage");
        Assert.Contains(result.EventLog, x => x.EventType == EventType.RestoreBarrier && x.Source == "effect.barrier");
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == friendly.Id);
        Assert.Equal(15, friendlyStats.BarrierGenerated);
        Assert.Equal(15, friendlyStats.Abilities.Single(x => x.Name == barrier.Name).TotalBarrier);
    }

    [Fact]
    public void Engine_can_start_active_abilities_on_their_cooldown()
    {
        var ability = CreateDamageAbility("ability.initial.cooldown", "Family.Test");
        ability.CooldownTicks = 3;
        var abilities = AbilityCompiler.CompileAbilities([ability]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 4,
                BasicAttackIntervalTicks: 1000,
                StartActiveAbilitiesOnCooldown: true));

        var result = engine.Run([friendly], [hostile]);

        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Timestamp == 0);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name && x.Timestamp == 3);
        Assert.Equal(178, hostile.Health);
    }

    [Fact]
    public void Engine_uses_fixed_basic_attack_cadence_regardless_of_precision()
    {
        var lowPrecision = CreateCombatant("low-precision", CombatTeam.Friendly, []);
        var baseline = CreateCombatant("baseline", CombatTeam.Hostile, []);
        lowPrecision.Attributes[AttributeType.Precision] = 5;
        baseline.Attributes[AttributeType.Precision] = 20;
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([lowPrecision], [baseline]);

        Assert.Equal(2, CountBasicAttacks(result, lowPrecision.Id));
        Assert.Equal(2, CountBasicAttacks(result, baseline.Id));
    }

    [Fact]
    public void Engine_attack_speed_increases_basic_attack_cadence()
    {
        var hasted = CreateCombatant("hasted", CombatTeam.Friendly, []);
        var baseline = CreateCombatant("baseline", CombatTeam.Hostile, []);
        hasted.Attributes[AttributeType.AttackSpeed] = 100;
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([hasted], [baseline]);

        Assert.Equal(4, CountBasicAttacks(result, hasted.Id));
        Assert.Equal(2, CountBasicAttacks(result, baseline.Id));
    }

    [Fact]
    public void Engine_applies_recipe_variant_basic_attack_behavior()
    {
        var variantWeapon = new RuntimeCombatant(
            "variant-weapon",
            "Variant Weapon",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 500,
                [AttributeType.Power] = 100
            },
            [],
            basicAttackIntervalMultiplier: 0.5d,
            basicAttackDamageMultiplier: 2d,
            basicAttackType: AttackType.Ranged,
            basicAttackDamageType: DamageType.Magical);
        var baseline = CreateCombatant("baseline", CombatTeam.Hostile, [], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([variantWeapon], [baseline]);

        Assert.Equal(4, CountBasicAttacks(result, variantWeapon.Id));
        Assert.Equal(2, CountBasicAttacks(result, baseline.Id));
        var basicAttackDamage = result.EventLog
            .Where(log =>
                log.ActorId == variantWeapon.Id &&
                log.Source == "Basic Attack" &&
                log.EventType == EventType.Damage)
            .Select(log => log.Magnitude)
            .ToList();
        Assert.All(basicAttackDamage, damage => Assert.InRange(damage, 81, 123));
        Assert.True(basicAttackDamage.Distinct().Count() > 1);
    }

    [Fact]
    public void Engine_varies_power_scaled_damage_and_healing_but_not_fixed_damage()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.combat.variance",
            Kind = AbilitySpecKind.Active,
            Name = "Combat Variance",
            Effects =
            [
                new()
                {
                    Id = "effect.power.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 50,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f,
                    AttackType = AttackType.None,
                    DamageType = DamageType.None
                },
                new()
                {
                    Id = "effect.fixed.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    AttackType = AttackType.None,
                    DamageType = DamageType.None
                },
                new()
                {
                    Id = "effect.power.heal",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 50,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f
                }
            ]
        };
        var compiledAbility = AbilityCompiler.CompileAbility(ability);
        var damageRolls = new List<int>();
        var healingRolls = new List<int>();

        foreach (var seed in Enumerable.Range(1, 20))
        {
            var friendly = CreateCombatant(
                $"friendly-{seed}",
                CombatTeam.Friendly,
                [compiledAbility],
                maxHealth: 1_000);
            friendly.AdjustHealth(-200);
            var hostile = CreateCombatant(
                $"hostile-{seed}",
                CombatTeam.Hostile,
                [],
                maxHealth: 1_000);
            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(
                    MaxTicks: 1,
                    BasicAttackIntervalTicks: 1_000,
                    RandomSeed: seed));

            var result = engine.Run([friendly], [hostile]);

            damageRolls.Add(Assert.Single(
                result.EventLog,
                log => log.Source == "effect.power.damage"
                       && log.EventType == EventType.Damage).Magnitude);
            healingRolls.Add(Assert.Single(
                result.EventLog,
                log => log.Source == "effect.power.heal"
                       && log.EventType == EventType.Heal).Magnitude);
            Assert.Equal(
                100,
                Assert.Single(
                    result.EventLog,
                    log => log.Source == "effect.fixed.damage"
                           && log.EventType == EventType.Damage).Magnitude);
        }

        Assert.All(damageRolls, damage => Assert.InRange(damage, 80, 120));
        Assert.All(healingRolls, healing => Assert.InRange(healing, 80, 120));
        Assert.True(damageRolls.Distinct().Count() > 1);
        Assert.True(healingRolls.Distinct().Count() > 1);
    }

    [Fact]
    public void Engine_attack_speed_decreases_basic_attack_cadence()
    {
        var slowed = CreateCombatant("slowed", CombatTeam.Friendly, []);
        var baseline = CreateCombatant("baseline", CombatTeam.Hostile, []);
        slowed.Attributes[AttributeType.AttackSpeed] = -50;
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([slowed], [baseline]);

        Assert.Equal(1, CountBasicAttacks(result, slowed.Id));
        Assert.Equal(2, CountBasicAttacks(result, baseline.Id));
    }

    [Fact]
    public void Engine_timed_attack_speed_buff_affects_current_battle_progress()
    {
        var hasteStatus = CreateTimedAttackSpeedStatus("status.haste", "effect.haste", 100, 30);
        var applyHaste = new AbilitySpec
        {
            Id = "ability.apply.haste",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Haste",
            CooldownTicks = 100,
            Effects = [CreateApplyStatusEffect("effect.apply.haste", hasteStatus.Id, AbilityTargetSelector.Self)]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([hasteStatus]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([applyHaste]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 31, BasicAttackIntervalTicks: 30));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(2, CountBasicAttacks(result, friendly.Id));
        Assert.Equal(0, friendly.GetAttribute(AttributeType.AttackSpeed));
        Assert.Contains(result.EventLog, x => x.Source == "effect.haste" && x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.Source == "effect.haste" && x.EventType == EventType.BuffExpired);
    }

    [Fact]
    public void Engine_timed_attack_speed_debuff_affects_current_battle_progress()
    {
        var slowStatus = CreateTimedAttackSpeedStatus("status.slow", "effect.slow", -50, 100);
        var applySlow = new AbilitySpec
        {
            Id = "ability.apply.slow",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Slow",
            CooldownTicks = 100,
            Effects = [CreateApplyStatusEffect("effect.apply.slow", slowStatus.Id)]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([slowStatus]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([applySlow]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(2, CountBasicAttacks(result, friendly.Id));
        Assert.Equal(1, CountBasicAttacks(result, hostile.Id));
        Assert.Contains(result.EventLog, x => x.Source == "effect.slow" && x.EventType == EventType.Debuff);
    }

    [Fact]
    public void Engine_stunned_combatants_do_not_gain_basic_attack_progress()
    {
        var stunStatus = CreateStunStatus();
        var compiledStatuses = AbilityCompiler.CompileStatuses([stunStatus]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Statuses.Add(new RuntimeStatus(compiledStatuses[stunStatus.Id], hostile, friendly, 1));
        var engine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 30));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(1, CountBasicAttacks(result, friendly.Id));
        Assert.Equal(2, CountBasicAttacks(result, hostile.Id));
    }

    [Fact]
    public void Engine_clamps_extreme_attack_speed_rates()
    {
        var veryFast = CreateCombatant("very-fast", CombatTeam.Friendly, []);
        var verySlow = CreateCombatant("very-slow", CombatTeam.Hostile, [], maxHealth: 2_000);
        veryFast.Attributes[AttributeType.AttackSpeed] = 1000;
        verySlow.Attributes[AttributeType.AttackSpeed] = -1000;
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 121, BasicAttackIntervalTicks: 30));

        var result = engine.Run([veryFast], [verySlow]);

        Assert.Equal(16, CountBasicAttacks(result, veryFast.Id));
        Assert.Equal(1, CountBasicAttacks(result, verySlow.Id));
    }

    [Fact]
    public void Engine_regenerates_health_every_five_seconds_regardless_of_spirit()
    {
        var lowSpirit = CreateCombatant("low-spirit", CombatTeam.Friendly, []);
        var highSpirit = CreateCombatant("high-spirit", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        lowSpirit.Attributes[AttributeType.HealthRegeneration] = 3;
        lowSpirit.Attributes[AttributeType.Spirit] = 0;
        highSpirit.Attributes[AttributeType.HealthRegeneration] = 3;
        highSpirit.Attributes[AttributeType.Spirit] = 1_000;
        lowSpirit.AdjustHealth(-20);
        highSpirit.AdjustHealth(-20);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 100, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([lowSpirit, highSpirit], [hostile]);

        Assert.Equal(186, lowSpirit.Health);
        Assert.Equal(186, highSpirit.Health);

        var lowSpiritRegeneration = result.EventLog
            .Where(x => x.EventType == EventType.HealthRegeneration && x.TargetId == lowSpirit.Id)
            .ToList();
        var highSpiritRegeneration = result.EventLog
            .Where(x => x.EventType == EventType.HealthRegeneration && x.TargetId == highSpirit.Id)
            .ToList();

        Assert.Equal([49, 99], lowSpiritRegeneration.Select(x => x.Timestamp));
        Assert.Equal([49, 99], highSpiritRegeneration.Select(x => x.Timestamp));
        Assert.All(lowSpiritRegeneration, x => Assert.Equal(3, x.Magnitude));
        Assert.All(highSpiritRegeneration, x => Assert.Equal(3, x.Magnitude));
        var lowSpiritStats = result.EntityStats.Single(x => x.EntityId == lowSpirit.Id);
        var highSpiritStats = result.EntityStats.Single(x => x.EntityId == highSpirit.Id);
        Assert.Equal(6, lowSpiritStats.HealthRegenerated);
        Assert.Equal(6, lowSpiritStats.HealthRegenerationPotential);
        Assert.Equal(0, lowSpiritStats.HealthRegenerationOverhealed);
        Assert.Equal(2, lowSpiritStats.HealthRegenerationPulses);
        Assert.Equal(6, highSpiritStats.HealthRegenerated);
        Assert.Equal(6, highSpiritStats.HealthRegenerationPotential);
        Assert.Equal(0, highSpiritStats.HealthRegenerationOverhealed);
        Assert.Equal(2, highSpiritStats.HealthRegenerationPulses);
    }

    [Fact]
    public void Engine_does_not_regenerate_health_before_five_seconds()
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Attributes[AttributeType.HealthRegeneration] = 3;
        friendly.AdjustHealth(-20);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 49, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(180, friendly.Health);
        Assert.DoesNotContain(
            result.EventLog,
            x => x.EventType == EventType.HealthRegeneration && x.TargetId == friendly.Id);
        Assert.Equal(
            0,
            result.EntityStats.SingleOrDefault(x => x.EntityId == friendly.Id)?.HealthRegenerated ?? 0);
        Assert.Equal(
            0,
            result.EntityStats.SingleOrDefault(x => x.EntityId == friendly.Id)?.HealthRegenerationPulses ?? 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Engine_ignores_non_positive_health_regeneration(int regeneration)
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Attributes[AttributeType.HealthRegeneration] = regeneration;
        friendly.AdjustHealth(-1);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 50, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(199, friendly.Health);
        Assert.DoesNotContain(
            result.EventLog,
            x => x.EventType == EventType.HealthRegeneration && x.TargetId == friendly.Id);
        Assert.Equal(
            0,
            result.EntityStats.SingleOrDefault(x => x.EntityId == friendly.Id)?.HealthRegenerated ?? 0);
        Assert.Equal(
            0,
            result.EntityStats.SingleOrDefault(x => x.EntityId == friendly.Id)?.HealthRegenerationPotential ?? 0);
    }

    [Fact]
    public void Engine_does_not_regenerate_health_at_full_health()
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Attributes[AttributeType.HealthRegeneration] = 3;
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 50, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        Assert.DoesNotContain(
            result.EventLog,
            x => x.EventType == EventType.HealthRegeneration && x.TargetId == friendly.Id);
        var stats = result.EntityStats.Single(x => x.EntityId == friendly.Id);
        Assert.Equal(0, stats.HealthRegenerated);
        Assert.Equal(3, stats.HealthRegenerationPotential);
        Assert.Equal(3, stats.HealthRegenerationOverhealed);
        Assert.Equal(1, stats.HealthRegenerationPulses);
    }

    [Fact]
    public void Engine_caps_health_regeneration_at_maximum_health()
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Attributes[AttributeType.HealthRegeneration] = 3;
        friendly.AdjustHealth(-1);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 50, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        var regeneration = Assert.Single(result.EventLog, x =>
            x.EventType == EventType.HealthRegeneration && x.TargetId == friendly.Id);
        Assert.Equal(1, regeneration.Magnitude);
        Assert.Equal(200, regeneration.CombatEntity!.Health);
        var stats = result.EntityStats.Single(x => x.EntityId == friendly.Id);
        Assert.Equal(1, stats.HealthRegenerated);
        Assert.Equal(3, stats.HealthRegenerationPotential);
        Assert.Equal(2, stats.HealthRegenerationOverhealed);
        Assert.Equal(1, stats.HealthRegenerationPulses);
    }

    [Fact]
    public void Engine_pays_health_cost_before_using_active_ability()
    {
        var ability = CreateDamageAbility("ability.health.cost", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Health,
            BaseValue = 25
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(175, friendly.Health);
        Assert.Equal(178, hostile.Health);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_does_not_use_active_ability_when_health_cost_cannot_be_paid()
    {
        var ability = CreateDamageAbility("ability.health.cost.unpaid", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Health,
            BaseValue = 200
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(200, friendly.Health);
        Assert.Equal(200, hostile.Health);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_recognizes_mana_costs_as_unpayable_until_mana_runtime_exists()
    {
        var ability = CreateDamageAbility("ability.mana.cost", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Mana,
            BaseValue = 5
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(200, friendly.Health);
        Assert.Equal(200, hostile.Health);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_stats_record_actual_health_damage_not_overkill()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.overkill",
            Kind = AbilitySpecKind.Active,
            Name = "Overkill",
            Effects =
            [
                new()
                {
                    Id = "effect.overkill.damage",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 20
                }
            ]
        };
        var abilities = AbilityCompiler.CompileAbilities([ability]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 5);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        var damageEvent = Assert.Single(result.EventLog, x => x.Source == "effect.overkill.damage" && x.EventType == EventType.Damage);
        Assert.Equal(5, damageEvent.Magnitude);
        Assert.Equal(5, result.EntityStats.Single(x => x.EntityId == "friendly").DamageDone);
        Assert.Equal(5, result.EntityStats.Single(x => x.EntityId == "hostile").DamageTaken);
    }

    [Fact]
    public void Engine_weights_taunting_targets_for_basic_attacks()
    {
        var front = CreateCombatant("front", CombatTeam.Friendly, []);
        var taunter = CreateCombatant("taunter", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        taunter.Conditions.Add(
            new RuntimeCondition(
                StandardConditionType.Taunt,
                taunter,
                taunter,
                1,
                30,
                taunter.GetAttribute(AttributeType.Power),
                1,
                "condition.taunt"));
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([front, taunter], [hostile]);

        Assert.Contains(result.EventLog, x =>
            x.ActorId == "hostile"
            && x.Source == "Basic Attack"
            && x.EventType == EventType.Damage
            && x.TargetId == "taunter");
        Assert.DoesNotContain(result.EventLog, x =>
            x.ActorId == "hostile"
            && x.Source == "Basic Attack"
            && x.EventType == EventType.Damage
            && x.TargetId == "front");
    }

    [Fact]
    public void Engine_supports_real_catalog_selectors()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.cleave",
                    Kind = AbilitySpecKind.Active,
                    Name = "Cleave",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.two.enemies",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.TwoEnemies,
                            BaseValue = 10
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.group.guard",
                    Kind = AbilitySpecKind.Active,
                    Name = "Group Guard",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.two.allies",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.TwoAllies,
                            BaseValue = 5
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.protect.large",
                    Kind = AbilitySpecKind.Active,
                    Name = "Protect Large",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.highest.max.health",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.HighestMaxHealthAlly,
                            BaseValue = 9
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var highHealthAlly = CreateCombatant("high-health-ally", CombatTeam.Friendly, [], maxHealth: 300);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, []);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, []);
        var thirdHostile = CreateCombatant("hostile-3", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly, ally, highHealthAlly], [firstHostile, secondHostile, thirdHostile]);

        Assert.Equal(2, new[] { firstHostile, secondHostile, thirdHostile }.Count(x => x.Health < x.GetAttribute(AttributeType.MaxHealth)));
        Assert.Equal(
            new[] { "hostile-1", "hostile-2" },
            result.EventLog
                .Where(x => x.Source == "effect.two.enemies" && x.EventType == EventType.Damage)
                .Select(x => x.TargetId)
                .ToArray());
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "effect.two.allies" && x.EventType == EventType.RestoreBarrier));
        Assert.Single(result.EventLog, x => x.Source == "effect.highest.max.health" && x.TargetId == "high-health-ally");
        Assert.Equal(9, highHealthAlly.Barrier);
    }

    [Fact]
    public void Engine_supports_restore_resource_lifesteal_and_real_catalog_events()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.self.wound",
                    Kind = AbilitySpecKind.Active,
                    Name = "Self Wound",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.self.wound",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 50
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.restore.barrier",
                    Kind = AbilitySpecKind.Active,
                    Name = "Restore Barrier",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.restore.barrier",
                            Operation = AbilityEffectOperation.RestoreResource,
                            Target = AbilityTargetSelector.Self,
                            Resource = AbilityResourceType.Barrier,
                            BaseValue = 12
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.life.drain",
                    Kind = AbilitySpecKind.Active,
                    Name = "Life Drain",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.life.drain",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 40,
                            AttackType = AttackType.Melee,
                            LifeStealPercentage = 50
                        }
                    ]
                },
                CreatePassiveBarrier("ability.on.melee", "effect.on.melee", AbilityTriggerEvent.OnMeleeAttack, 3),
                CreatePassiveBarrier("ability.on.health.changed", "effect.on.health.changed", AbilityTriggerEvent.OnHealthChanged, 4),
                CreatePassiveBarrier("ability.on.heal", "effect.on.heal", AbilityTriggerEvent.OnHeal, 5),
                CreatePassiveBarrier("ability.on.lifesteal", "effect.on.lifesteal", AbilityTriggerEvent.OnLifestealHeal, 6)
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Contains(result.EventLog, x => x.Source == "effect.restore.barrier" && x.EventType == EventType.RestoreBarrier && x.Magnitude == 12);
        Assert.Contains(result.EventLog, x => x.Source == "effect.life.drain" && x.EventType == EventType.Heal && x.Magnitude == 20);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.melee" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.health.changed" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.heal" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.lifesteal" && x.EventType == EventType.RestoreBarrier);
    }

    [Fact]
    public void Engine_limits_melee_attacked_passives_to_the_attacked_owner()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.melee.tap",
                    Kind = AbilitySpecKind.Active,
                    Name = "Melee Tap",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.melee.tap",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 1,
                            AttackType = AttackType.Melee
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.hot.aura",
                    Kind = AbilitySpecKind.Passive,
                    Name = "Hot Aura",
                    Triggers = [new() { Event = AbilityTriggerEvent.OnMeleeAttacked }],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.hot_aura.damage",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.EventTarget,
                            BaseValue = 4,
                            AttackType = AttackType.None,
                            DamageType = DamageType.Burn
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 200);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.DoesNotContain(result.EventLog, x =>
            x.Source == "effect.hot_aura.damage"
            && x.TargetId == "friendly");
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.hot_aura.damage"
            && x.TargetId == "hostile"
            && x.Magnitude == 4);

        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly");
        var hotAura = friendlyStats.Abilities.Single(x => x.Name == "Hot Aura");
        Assert.Equal(4, hotAura.TotalDamage);
    }

    [Fact]
    public void Engine_supports_cooldown_restore_resource()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.execute",
                    Kind = AbilitySpecKind.Active,
                    Name = "Execute",
                    CooldownTicks = 20,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.execute",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 50,
                            AttackType = AttackType.Melee
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.on.kill.cooldown",
                    Kind = AbilitySpecKind.Passive,
                    Name = "On Kill Cooldown",
                    Triggers =
                    [
                        new()
                        {
                            Event = AbilityTriggerEvent.OnKill,
                            EffectIds = [ "effect.restore.cooldown" ]
                        }
                    ],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.restore.cooldown",
                            Operation = AbilityEffectOperation.RestoreResource,
                            Target = AbilityTargetSelector.Self,
                            Resource = AbilityResourceType.Cooldown,
                            BaseValue = 5
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 20);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        var execute = friendly.Abilities.Single(x => x.Definition.Id == "ability.execute");
        Assert.True(execute.RemainingCooldownTicks < 19);
        Assert.Contains(result.EventLog, x => x.Source == "effect.restore.cooldown" && x.EventType == EventType.Buff);
    }

    [Fact]
    public void Engine_does_not_spend_active_cooldown_when_no_effect_can_resolve()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.requires.status",
                    Kind = AbilitySpecKind.Active,
                    Name = "Requires Status",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.requires.status",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 10,
                            Conditions =
                            [
                                new()
                                {
                                    Type = AbilityConditionType.HasStatus,
                                    Subject = AbilityConditionSubject.Target,
                                    StatusId = "status.missing"
                                }
                            ]
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        var ability = friendly.Abilities.Single(x => x.Definition.Id == "ability.requires.status");
        Assert.Equal(0, ability.RemainingCooldownTicks);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "friendly" && x.EventType == EventType.AbilityUse);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.requires.status");
    }

    [Fact]
    public void Engine_stops_using_active_abilities_after_last_opponent_dies()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.killing.blow",
                    Kind = AbilitySpecKind.Active,
                    Name = "Killing Blow",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.killing.blow",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 50
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.after.kill",
                    Kind = AbilitySpecKind.Active,
                    Name = "After Kill",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.after.kill",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 10
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 20);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        var killingBlow = friendly.Abilities.Single(x => x.Definition.Id == "ability.killing.blow");
        var afterKill = friendly.Abilities.Single(x => x.Definition.Id == "ability.after.kill");
        Assert.True(killingBlow.RemainingCooldownTicks > 0);
        Assert.Equal(0, afterKill.RemainingCooldownTicks);
        Assert.Contains(result.EventLog, x => x.Source == "effect.killing.blow" && x.EventType == EventType.Damage);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.after.kill");
        Assert.DoesNotContain(result.EventLog, x => x.Source == "After Kill" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public void Engine_honors_limited_uses_for_immediate_trigger_effects()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.self.wound",
                    Kind = AbilitySpecKind.Active,
                    Name = "Self Wound",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.self.wound",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 5
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.one.use.guard",
                    Kind = AbilitySpecKind.Passive,
                    Name = "One Use Guard",
                    Triggers = [new() { Event = AbilityTriggerEvent.OnHealthChanged }],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.one.use.guard",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 3,
                            Uses = 1
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.one.use.guard" && x.EventType == EventType.RestoreBarrier);
        Assert.Equal(3, friendly.Barrier);
    }

    [Fact]
    public void Engine_reserves_internal_cooldown_before_nested_heal_events()
    {
        var abilities = AbilityCompiler.CompileAbilities(
        [
            new AbilitySpec
            {
                Id = "ability.self.wound.for.heal.chain",
                Kind = AbilitySpecKind.Active,
                Name = "Self Wound",
                Triggers = [new() { Event = AbilityTriggerEvent.OnAbilityUsed, EffectIds = ["effect.self.wound.for.heal.chain"] }],
                Effects =
                [
                    new()
                    {
                        Id = "effect.self.wound.for.heal.chain",
                        Operation = AbilityEffectOperation.Damage,
                        Target = AbilityTargetSelector.Self,
                        BaseValue = 100
                    }
                ]
            },
            new AbilitySpec
            {
                Id = "ability.initial.heal",
                Kind = AbilitySpecKind.Active,
                Name = "Initial Heal",
                Triggers = [new() { Event = AbilityTriggerEvent.OnAbilityUsed, EffectIds = ["effect.initial.heal"] }],
                Effects =
                [
                    new()
                    {
                        Id = "effect.initial.heal",
                        Operation = AbilityEffectOperation.Heal,
                        Target = AbilityTargetSelector.Self,
                        BaseValue = 20
                    }
                ]
            },
            new AbilitySpec
            {
                Id = "ability.nested.heal.guard",
                Kind = AbilitySpecKind.Passive,
                Name = "Nested Heal Guard",
                Triggers =
                [
                    new()
                    {
                        Event = AbilityTriggerEvent.OnHeal,
                        InternalCooldownTicks = 1,
                        EffectIds = ["effect.nested.heal.guard"]
                    }
                ],
                Effects =
                [
                    new()
                    {
                        Id = "effect.nested.heal.guard",
                        Operation = AbilityEffectOperation.Heal,
                        Target = AbilityTargetSelector.Self,
                        BaseValue = 1
                    }
                ]
            }
        ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Single(result.EventLog, entry =>
            entry.Source == "effect.nested.heal.guard" && entry.EventType == EventType.Heal);
        Assert.Equal(121, friendly.Health);
    }

    [Fact]
    public void Engine_honors_limited_uses_across_multi_target_effects()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.limited.cleave",
                    Kind = AbilitySpecKind.Active,
                    Name = "Limited Cleave",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.limited.cleave",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.TwoEnemies,
                            BaseValue = 10,
                            Uses = 1
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, []);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [firstHostile, secondHostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.limited.cleave" && x.EventType == EventType.Damage);
        Assert.True(firstHostile.Health < firstHostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(secondHostile.GetAttribute(AttributeType.MaxHealth), secondHostile.Health);
    }

    [Fact]
    public void Engine_supports_dodge_triggers()
    {
        var friendlyAbilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.melee.strike",
                    Kind = AbilitySpecKind.Active,
                    Name = "Melee Strike",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.melee.strike",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 40,
                            AttackType = AttackType.Melee
                        }
                    ]
                }
            ]);
        var hostileAbilities = AbilityCompiler.CompileAbilities(
            [
                CreatePassiveBarrier("ability.on.dodge", "effect.on.dodge", AbilityTriggerEvent.OnDodge, 7)
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, friendlyAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values, dodgeChance: 100);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, RandomSeed: 7));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(hostile.GetAttribute(AttributeType.MaxHealth), hostile.Health);
        Assert.Contains(result.EventLog, x => x.Source == "effect.melee.strike" && x.EventType == EventType.Miss);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.dodge" && x.EventType == EventType.RestoreBarrier && x.TargetId == "hostile");
    }

    [Fact]
    public void Engine_applies_status_and_runs_damage_over_time()
    {
        var ignite = new AbilitySpec
        {
            Id = "ability.ignite",
            Kind = AbilitySpecKind.Active,
            Name = "Ignite",
            Effects =
            [
                new()
                {
                    Id = "effect.apply.burn",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.burn",
                    BaseValue = 1
                }
            ]
        };

        var result = RunBattle([ignite], [CreateBurnStatus()], maxTicks: 8, out _, out var hostile);

        Assert.Contains(hostile.Statuses, x => x.Definition.Id == "status.burn");
        Assert.True(result.EventLog.Count(x => x.Source == "effect.burn.dot" && x.EventType == EventType.Damage) >= 2);
    }

    [Fact]
    public void Engine_expires_timed_attribute_buffs()
    {
        var frenzy = new AbilitySpec
        {
            Id = "ability.frenzy",
            Kind = AbilitySpecKind.Active,
            Name = "Frenzy",
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "effect.power.buff",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.Self,
                    Attribute = AttributeType.Power,
                    BaseValue = 20,
                    DurationTicks = 3
                }
            ]
        };

        var result = RunBattle([frenzy], [], maxTicks: 4, out var friendly, out _);

        Assert.Equal(50, friendly.GetAttribute(AttributeType.Power));
        Assert.Contains(result.EventLog, x => x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.BuffExpired);
    }

    [Fact]
    public void Json_catalog_large_rat_big_increases_current_health_with_max_health()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.creature.large_rat.big"]]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        engine.Run([friendly], [hostile]);

        Assert.Equal(220, friendly.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(220, friendly.Health);
    }

    [Fact]
    public void Json_catalog_authors_proc_coefficients_for_all_effects()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilityEffects = catalog.AbilitiesById.Values.SelectMany(x => x.Effects);
        var statusEffects = catalog.Statuses.SelectMany(x => x.Effects);
        var effects = abilityEffects.Concat(statusEffects).ToArray();

        Assert.NotEmpty(effects);
        Assert.All(effects, effect => Assert.InRange(effect.ProcCoefficient, 0.01m, 2m));
        Assert.Contains(effects, effect => effect.ProcCoefficient < 1m);
        Assert.Contains(effects, effect => effect.Operation == AbilityEffectOperation.Damage && effect.ProcCoefficient < 1m);
    }

    [Fact]
    public void Json_creature_profiles_resolve_every_authored_native_ability()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var profiles = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var monsterIds = new[]
        {
            "vampire_bat", "raven", "venomous_snake", "nightshade_blossom", "blood_zombie",
            "lumo_wisp", "lumo_sentinel", "goblin", "goblin_archer", "goblin_warrior",
            "goblin_shaman", "hobgoblin", "frost_imp", "crystal_wisp", "blue_slime",
            "transparent_slime", "moss_lizard", "shadow_imp", "grave_hound", "lost_soul",
            "grave_wisp", "skeleton", "pixie", "wood_nymph", "rainbow_slime",
            "enchanted_fairy", "illusion_fox", "thornback_boar", "hollow_stag", "treant_sapling",
            "glade_panther", "forest_spirit", "rotroot_shambler", "spider", "giant_spider",
            "venomous_spiderling", "blackjaw_spider", "flame_imp", "smolder_rat", "cinder_beetle",
            "red_slime", "giant_worm", "bog_mite", "green_slime", "large_rat", "viper",
            "poisonous_rat", "rotfly_toad", "brown_slime", "cave_bat", "giant_bat", "undead"
        };

        var allAbilityIds = monsterIds
            .Select(id => (MonsterId: $"monster.{id}", AbilityIds: profiles.GetAbilityIds($"monster.{id}")))
            .ToArray();

        Assert.Equal(52, allAbilityIds.Length);
        Assert.All(allAbilityIds, profile =>
            Assert.Equal(profile.MonsterId == "monster.hobgoblin" ? 3 : 2, profile.AbilityIds.Count));
        Assert.Equal(105, allAbilityIds.SelectMany(x => x.AbilityIds).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(106, catalog.AbilitiesById.Count);
        Assert.Contains("ability.summon.shadow_image.shadow_strike", catalog.AbilitiesById.Keys);
        Assert.All(allAbilityIds.SelectMany(x => x.AbilityIds), abilityId =>
        {
            var ability = Assert.IsType<AbilitySpec>(catalog.AbilitiesById.GetValueOrDefault(abilityId));
            Assert.StartsWith("essence.", ability.OwningEssenceId, StringComparison.Ordinal);
            Assert.StartsWith("ability.creature.", ability.Id, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Json_essence_catalogue_contains_only_the_authored_creature_roster()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var definitions = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var lootTables = new JsonCreatureEssenceLootTableRepository(
            CreateConfig(),
            contentRoot,
            options,
            definitions);
        var itemPath = Path.Combine(contentRoot, "Data", "items", "items.json");
        var items = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(itemPath), options) ?? [];

        var allDefinitions = definitions.GetAll();
        var allLootTables = lootTables.GetAll();
        var essenceItems = items.Where(item =>
            item.TryGetProperty("itemType", out var itemType)
            && itemType.GetString()?.Equals("Essence", StringComparison.OrdinalIgnoreCase) == true).ToList();

        Assert.Equal(53, allDefinitions.Count);
        Assert.Equal(52, allDefinitions.Select(x => x.SourceMonsterId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(52, allLootTables.Count);
        Assert.Equal(53, essenceItems.Count);
        Assert.All(allDefinitions, definition =>
        {
            Assert.StartsWith("monster.", definition.SourceMonsterId, StringComparison.Ordinal);
            Assert.StartsWith("ability.creature.", definition.ActiveAbilityId, StringComparison.Ordinal);
            Assert.StartsWith("ability.creature.", definition.PassiveAbilityId, StringComparison.Ordinal);
        });
        Assert.Equal(2, allDefinitions.Count(x => x.SourceMonsterId == "monster.hobgoblin"));
    }

    [Fact]
    public void Basic_attack_passives_modify_the_attack_that_triggered_them()
    {
        var passive = new AbilitySpec
        {
            Id = "ability.test.basic_attack_modifier",
            Kind = AbilitySpecKind.Passive,
            Name = "Basic Attack Modifier",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnBasicAttack,
                    EffectIds = ["effect.test.basic_damage", "effect.test.basic_penetration"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.test.basic_damage",
                    Operation = AbilityEffectOperation.ModifyNextBasicAttackDamage,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 40
                },
                new()
                {
                    Id = "effect.test.basic_penetration",
                    Operation = AbilityEffectOperation.ModifyNextBasicAttackArmorPenetration,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 50
                }
            ]
        };
        var compiled = AbilityCompiler.CompileAbilities([passive]);
        var attacker = CreateCombatant("attacker", CombatTeam.Friendly, compiled.Values);
        var defender = CreateCombatant("defender", CombatTeam.Hostile, []);
        defender.AdjustAttribute(AttributeType.Armor, 100);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(
            MaxTicks: 1,
            BasicAttackIntervalTicks: 1,
            RandomSeed: 17));

        var result = engine.Run([attacker], [defender]);

        var hit = Assert.Single(result.EventLog, x =>
            x.Source == "Basic Attack"
            && x.ActorId == "attacker"
            && x.EventType == EventType.Damage);
        Assert.True(hit.Magnitude > 0);
        Assert.Contains(result.EventLog, x => x.Source == "effect.test.basic_damage" && x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.Source == "effect.test.basic_penetration" && x.EventType == EventType.Buff);
        var remainingModifiers = attacker.ConsumeNextBasicAttackModifiers();
        Assert.Equal(0, remainingModifiers.DamagePercent);
        Assert.Equal(0, remainingModifiers.ArmorPenetration);
    }

    [Fact]
    public void Json_creature_essence_loot_tables_resolve_all_authored_essences()
    {
        var definitions = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var lootTables = new JsonCreatureEssenceLootTableRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            definitions);

        Assert.Equal(definitions.GetAll().Count, lootTables.GetAll().Sum(x => x.Variants.Count));
        Assert.All(lootTables.GetAll(), table =>
        {
            Assert.InRange(table.BaseDropChance, double.Epsilon, 1);
            Assert.Equal(AbilitySpecKind.Passive, definitions.GetAbilityById(table.PassiveAbilityId)?.Kind);
            Assert.NotEmpty(table.Variants);
            Assert.All(table.Variants, variant =>
            {
                var essence = Assert.IsType<EssenceDefinition>(definitions.GetById(variant.EssenceDefinitionId));
                Assert.Equal(table.PassiveAbilityId, essence.PassiveAbilityId);
                Assert.Equal(variant.ActiveAbilityId, essence.ActiveAbilityId);
                Assert.Same(table, lootTables.GetByEssenceDefinitionId(variant.EssenceDefinitionId));
            });
        });
    }

    [Fact]
    public void Balance_simulator_ranks_random_essence_combinations()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var simulator = new AbilityBalanceSimulator(provider, essenceRepository);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 20,
            TeamSize: 2,
            EssencesPerParticipant: 2,
            RandomSeed: 123,
            TopResults: 10,
            CandidatePoolSize: 5,
            CandidateTeams: null));

        Assert.Equal("RandomPool", report.Mode);
        Assert.Equal(20, report.BattlesRun);
        Assert.Equal(2, report.TeamSize);
        Assert.Equal(2, report.EssencesPerParticipant);
        Assert.Equal(5, report.CandidatePoolSize);
        Assert.Equal(5, report.CandidateTeamCount);
        Assert.NotEmpty(report.RankedCombinations);
        Assert.True(report.RankedCombinations.Count <= 5);
        Assert.Equal(40, report.RankedCombinations.Sum(x => x.Battles));
        Assert.All(report.RankedCombinations, combination =>
        {
            Assert.DoesNotContain("essence.", combination.DisplayName, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(combination.Signature, combination.DisplayName);
        });
        Assert.All(report.RankedCombinations, combination =>
        {
            Assert.True(combination.Battles > 0);
            Assert.InRange(combination.WinRate, 0, 1);
            Assert.Equal(2, combination.Participants.Count);
            Assert.All(combination.Participants, participant => Assert.Equal(2, participant.EssenceIds.Count));
        });
    }

    [Fact]
    public void Balance_simulator_runs_saved_combinations_as_round_robin()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var simulator = new AbilityBalanceSimulator(provider, essenceRepository);
        var first = new AbilityBalanceTeamLoadout(
            [new AbilityBalanceParticipantLoadout(["essence.large_rat"])]);
        var second = new AbilityBalanceTeamLoadout(
            [new AbilityBalanceParticipantLoadout(["essence.flame_imp"])]);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 6,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: 456,
            TopResults: 10,
            CandidatePoolSize: 10,
            CandidateTeams: [first, second]));

        Assert.Equal("SavedRoundRobin", report.Mode);
        Assert.Equal(6, report.BattlesRun);
        Assert.Equal(2, report.CandidateTeamCount);
        Assert.Equal(10, report.CandidatePoolSize);
        Assert.Equal(2, report.RankedCombinations.Count);
        Assert.Contains(report.RankedCombinations, combination => combination.DisplayName == "Large Rat Essence");
        Assert.Contains(report.RankedCombinations, combination => combination.DisplayName == "Flame Imp Essence");
        Assert.All(report.RankedCombinations, combination => Assert.Equal(6, combination.Battles));
    }

    [Fact]
    public void Engine_supports_status_stacks_and_reflect_triggers()
    {
        var thorns = new AbilitySpec
        {
            Id = "ability.thorns",
            Kind = AbilitySpecKind.Active,
            Name = "Thorns",
            Effects =
            [
                new()
                {
                    Id = "effect.apply.thorns",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.Self,
                    StatusId = "status.thorns",
                    BaseValue = 1
                }
            ]
        };

        var result = RunBattle([thorns], [CreateThornsStatus()], maxTicks: 35, out _, out var hostile);

        Assert.Contains(result.EventLog, x => x.Source == "effect.thorns.reflect" && x.EventType == EventType.Damage);
        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
    }

    [Fact]
    public void Engine_applies_damage_reduction_before_health_damage()
    {
        var hostileAbility = CreateDamageAbility("ability.reduced_hit", "Family.Test");
        var compiledAbilities = AbilityCompiler.CompileAbilities([hostileAbility]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, compiledAbilities.Values);
        friendly.AdjustAttribute(AttributeType.DamageReduction, 25);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(184, friendly.Health);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage" && x.EventType == EventType.Damage && x.Magnitude == 16);
    }

    [Fact]
    public void Json_catalog_illusion_fox_foxfire_only_retaliates_when_owner_is_attacked()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.creature.illusion_fox.foxfire_wisp"]]);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);

        var foxOwner = CreateCombatant("fox-owner", CombatTeam.Friendly, compiledAbilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var attacker = CreateCombatant("attacker", CombatTeam.Hostile, []);
        var allyTargetedEngine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 60));

        var allyTargetedResult = allyTargetedEngine.Run([ally, foxOwner], [attacker]);

        Assert.Contains(allyTargetedResult.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.ActorId == "fox-owner"
            && x.TargetId == "fox-owner"
            && x.EventType == EventType.StatusEffect);
        Assert.DoesNotContain(allyTargetedResult.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.TargetId == "ally");
        Assert.DoesNotContain(allyTargetedResult.EventLog, x => x.Source == "effect.foxfire.damage");

        foxOwner = CreateCombatant("fox-owner", CombatTeam.Friendly, compiledAbilities.Values);
        ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        attacker = CreateCombatant("attacker", CombatTeam.Hostile, []);
        var ownerTargetedEngine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 60));

        var ownerTargetedResult = ownerTargetedEngine.Run([foxOwner, ally], [attacker]);

        Assert.Contains(ownerTargetedResult.EventLog, x =>
            x.Source == "effect.foxfire.damage"
            && x.ActorId == "fox-owner"
            && x.TargetId == "attacker"
            && x.EventType == EventType.Damage
            && x.Magnitude is >= 14 and <= 22);
        Assert.Equal(0, foxOwner.GetStatusStacks("status.foxfire_stack"));
    }

    [Fact]
    public void Engine_stunned_combatants_skip_active_abilities_and_basic_attacks()
    {
        var hostileAbility = CreateDamageAbility("ability.stunned.hit", "Family.Test");
        var statuses = AbilityCompiler.CompileStatuses([CreateStunStatus()]);
        var hostileAbilities = AbilityCompiler.CompileAbilities([hostileAbility]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values);
        hostile.Statuses.Add(new RuntimeStatus(statuses["status.stunned"], hostile, hostile, 1));
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "hostile" && x.EventType is EventType.AbilityUse or EventType.Damage);
    }

    [Fact]
    public void Json_catalog_feral_pounce_stun_blocks_actions()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var hostileAbilities = AbilityCompiler.CompileAbilities([CreateDamageAbility("ability.hostile.hit", "Family.Test")]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values);
        hostile.Conditions.Add(
            new RuntimeCondition(
                StandardConditionType.Stun,
                hostile,
                hostile,
                1,
                30,
                hostile.GetAttribute(AttributeType.Power),
                1,
                "condition.stun"));
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "hostile" && x.EventType is EventType.AbilityUse or EventType.Damage);
    }

    [Fact]
    public void Engine_refresh_status_reapplies_duration_without_stacking()
    {
        var refreshStatus = CreateEmptyStatus("status.refresh", AbilityStatusStackingPolicy.Refresh, maxStacks: 5, durationTicks: 10);
        var applyTwice = new AbilitySpec
        {
            Id = "ability.apply.refresh.twice",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Refresh Twice",
            Effects =
            [
                CreateApplyStatusEffect("effect.apply.refresh.one", "status.refresh"),
                CreateApplyStatusEffect("effect.apply.refresh.two", "status.refresh")
            ]
        };

        var result = RunBattle([applyTwice], [refreshStatus], maxTicks: 1, out _, out var hostile);

        Assert.Equal(1, hostile.GetStatusStacks("status.refresh"));
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "status.refresh" && x.EventType == EventType.StatusEffect));
    }

    [Fact]
    public void Engine_stack_status_accumulates_to_max_stacks()
    {
        var stackStatus = CreateEmptyStatus("status.stack", AbilityStatusStackingPolicy.Stack, maxStacks: 3, durationTicks: 10);
        var applyFourTimes = new AbilitySpec
        {
            Id = "ability.apply.stack.four",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Stack Four",
            Effects =
            [
                CreateApplyStatusEffect("effect.apply.stack.one", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.two", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.three", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.four", "status.stack")
            ]
        };

        RunBattle([applyFourTimes], [stackStatus], maxTicks: 1, out _, out var hostile);

        Assert.Equal(3, hostile.GetStatusStacks("status.stack"));
    }

    [Fact]
    public void Engine_modify_status_stacks_to_zero_removes_status()
    {
        var stackStatus = CreateEmptyStatus("status.stack.consume", AbilityStatusStackingPolicy.Stack, maxStacks: 3, durationTicks: 10);
        var consume = new AbilitySpec
        {
            Id = "ability.consume.stack",
            Kind = AbilitySpecKind.Active,
            Name = "Consume Stack",
            Effects =
            [
                new()
                {
                    Id = "effect.consume.stack",
                    Operation = AbilityEffectOperation.ModifyStatusStacks,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.stack.consume",
                    BaseValue = -1
                }
            ]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([stackStatus]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([consume]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.stack.consume"], hostile, hostile, 1));
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Empty(hostile.Statuses);
        Assert.Contains(result.EventLog, x => x.Source == "status.stack.consume" && x.EventType == EventType.StatusEffectRemoved);
    }

    [Fact]
    public void Engine_remove_status_and_cleanse_clear_statuses()
    {
        var removable = CreateEmptyStatus("status.removable", AbilityStatusStackingPolicy.Refresh, maxStacks: 1, durationTicks: 100);
        var lingering = CreateEmptyStatus("status.lingering", AbilityStatusStackingPolicy.Refresh, maxStacks: 1, durationTicks: 100);
        var removeAndCleanse = new AbilitySpec
        {
            Id = "ability.remove.cleanse",
            Kind = AbilitySpecKind.Active,
            Name = "Remove And Cleanse",
            Effects =
            [
                new()
                {
                    Id = "effect.remove.status",
                    Operation = AbilityEffectOperation.RemoveStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.removable"
                },
                new()
                {
                    Id = "effect.cleanse",
                    Operation = AbilityEffectOperation.Cleanse,
                    Target = AbilityTargetSelector.CurrentTarget
                }
            ]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([removable, lingering]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([removeAndCleanse]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.removable"], hostile, hostile, 1));
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.lingering"], hostile, hostile, 1));
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Empty(hostile.Statuses);
        Assert.Contains(result.EventLog, x => x.Source == "effect.remove.status" && x.EventType == EventType.StatusEffectRemoved);
        Assert.Contains(result.EventLog, x => x.Source == "effect.cleanse" && x.EventType == EventType.StatusEffectCleansed);
    }

    [Fact]
    public void Engine_status_applied_timed_attribute_buff_expires_cleanly()
    {
        var buffStatus = CreateTimedPowerBuffStatus();
        var applyBuff = new AbilitySpec
        {
            Id = "ability.apply.power.status",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Power Status",
            CooldownTicks = 100,
            Effects = [CreateApplyStatusEffect("effect.apply.power.status", buffStatus.Id, AbilityTargetSelector.Self)]
        };

        var result = RunBattle([applyBuff], [buffStatus], maxTicks: 4, out var friendly, out _);

        Assert.Equal(50, friendly.GetAttribute(AttributeType.Power));
        Assert.Contains(result.EventLog, x => x.Source == "effect.status.power.buff" && x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.Source == "effect.status.power.buff" && x.EventType == EventType.BuffExpired);
    }

    [Fact]
    public void Engine_is_seed_deterministic()
    {
        var ability = CreateDamageAbility("ability.seeded", "Family.Test");

        var first = RunBattle([ability], [], maxTicks: 5, out _, out _, seed: 99);
        var second = RunBattle([ability], [], maxTicks: 5, out _, out _, seed: 99);

        Assert.Equal(
            first.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, x.Magnitude)).ToList(),
            second.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, x.Magnitude)).ToList());
    }

    [Fact]
    public void Json_catalog_loads_compiles_and_runs_seeded_battle()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();

        Assert.Contains("ability.creature.goblin.shiv_jab", catalog.AbilitiesById.Keys);
        Assert.DoesNotContain("status.training.burn", catalog.StatusesById.Keys);
        Assert.Equal("essence.goblin", catalog.OwningEssenceByAbilityId["ability.creature.goblin.shiv_jab"]);

        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [
                catalog.AbilitiesById["ability.creature.goblin.shiv_jab"],
                catalog.AbilitiesById["ability.creature.lumo_wisp.lumo_barrier"],
                catalog.AbilitiesById["ability.creature.flame_imp.firebomb_toss"]
            ]);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var friendly = CreateCombatant("json-friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("json-hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 70, RandomSeed: 7));

        var result = engine.Run([friendly], [hostile]);

        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Contains(result.EventLog, x => x.Source == "effect.creature.goblin.shiv_jab.damage" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "effect.creature.lumo_wisp.lumo_barrier.barrier" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "condition.burn" && x.EventType == EventType.Damage);
    }

    [Fact]
    public void Json_catalog_compiles_all_authored_specs()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();

        var compiledAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);

        Assert.Equal(catalog.Abilities.Count, compiledAbilities.Count);
        Assert.Equal(catalog.Statuses.Count, compiledStatuses.Count);
    }

    [Fact]
    public void Json_catalog_behavior_manifest_observations_pass()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var diagnostics = new AbilityCatalogBehaviorDiagnostics(
            provider,
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            essenceRepository);

        var report = diagnostics.Analyze();
        var failures = report.Scenarios
            .Where(x => !x.Passed)
            .SelectMany(x => x.Failures.Select(failure => $"{x.BehaviorId}/{x.AbilityId}: {failure}"))
            .ToList();

        Assert.True(report.ScenarioCount > 0);
        Assert.True(report.IsComplete, string.Join(Environment.NewLine, failures));
        Assert.Equal(report.ScenarioCount, report.PassedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.True(report.HasFullAbilityCoverage, string.Join(Environment.NewLine, report.MissingAbilityIds));
        Assert.Equal(report.AbilityCount, report.CoveredAbilityCount);
        Assert.Empty(report.MissingAbilityIds);
    }

    [Fact]
    public void Json_catalog_covers_authored_essence_slots()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var provider = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options);
        var analyzer = new AbilityCatalogCoverageAnalyzer(essenceRepository, provider);

        var report = analyzer.Analyze();

        Assert.True(report.IsComplete, string.Join(Environment.NewLine, report.Gaps.Select(x => $"{x.EssenceId} {x.Slot}: {x.Reason}")));
        Assert.Equal(report.RequiredSlotCount, report.CoveredSlotCount);
        Assert.Equal(106, report.RequiredSlotCount);
        Assert.Equal(53, report.EssenceCount);
        Assert.Equal(report.EssenceCount, report.RuntimeLoadoutChecks.Count);
        Assert.All(report.RuntimeLoadoutChecks, check =>
        {
            Assert.True(check.IsReady, $"{check.EssenceId}: {check.Failure}");
            Assert.Null(check.Failure);
            Assert.Equal(2, check.AbilityIds.Count);
        });
        Assert.Equal(
            essenceRepository.GetAll().Select(x => x.Id).Order(StringComparer.OrdinalIgnoreCase),
            report.RuntimeLoadoutChecks.Select(x => x.EssenceId).Order(StringComparer.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task Combat_engine_executor_runs_real_encounter_runtime()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var friendlyCharacter = CreateSourceCharacter("Executor Friendly");
        var hostileCharacter = CreateSourceCharacter("Executor Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.goblin");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Equal(plan.StartsAt, result.StartedAt);
        Assert.True(result.Duration > 0);
        Assert.Contains(result.EventLog, x => x.Source == "effect.creature.goblin.shiv_jab.damage" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "condition.bleed" && x.EventType == EventType.Damage);
    }

    [Fact]
    public async Task Combat_engine_executor_uses_repeatable_encounter_specific_randomness()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);
        var firstEncounterId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondEncounterId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        async Task<int[]> RunEncounter(Guid encounterId)
        {
            var runtime = CreateTrainingEncounterRuntime(
                out _,
                out _,
                encounterId: encounterId);
            var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
            return result.EventLog
                .Where(log => log.EventType is EventType.Damage or EventType.DamageCrit)
                .Select(log => log.Magnitude)
                .ToArray();
        }

        var firstRun = await RunEncounter(firstEncounterId);
        var repeatedRun = await RunEncounter(firstEncounterId);
        var differentEncounter = await RunEncounter(secondEncounterId);

        Assert.Equal(firstRun, repeatedRun);
        Assert.NotEqual(firstRun, differentEncounter);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_evolved_conditional_multiplier_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.evolved_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Evolved Strike",
            OwningEssenceId = "essence.test.evolved",
            CooldownTicks = 700,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.status.bleed", "effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.status.bleed",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 1,
                    StatusId = "status.bleed"
                },
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var status = new StatusSpec
        {
            Id = "status.bleed",
            Name = "Bleed",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            DurationTicks = 100
        };
        var essence = new EssenceDefinition
        {
            Id = "essence.test.evolved",
            ActiveAbilityId = ability.Id,
            Evolution = new EssenceEvolutionDefinition
            {
                ActiveAbilityModifiers =
                [
                    new()
                    {
                        Target = "effect.damage.main",
                        Operation = "AddMultiplier",
                        Value = 0.5,
                        Condition = "TargetHasStatus.Bleed"
                    }
                ]
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [status],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = essence.Id
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var repository = new FakeLegacyDefinitionRepository([ability], [essence]);
        var friendlyCharacter = CreateSourceCharacter("Evolved Friendly");
        var hostileCharacter = CreateSourceCharacter("Evolved Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, essence.Id);
        friendlyCombatant.EquippedEssences.Single().IsEvolved = true;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        IncreaseMaxHealth(friendlyCombatant, 2_000);
        IncreaseMaxHealth(hostileCombatant, 2_000);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider, repository);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 10);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main.evolved_bonus" && x.EventType == EventType.Damage && x.Magnitude == 5);
        Assert.Contains(result.EventLog, x => x.Source == "Evolved Strike" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_evolved_add_effect_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.add_effect_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Add Effect Strike",
            OwningEssenceId = "essence.test.add_effect",
            CooldownTicks = 700,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var essence = new EssenceDefinition
        {
            Id = "essence.test.add_effect",
            ActiveAbilityId = ability.Id,
            Evolution = new EssenceEvolutionDefinition
            {
                ActiveAbilityModifiers =
                [
                    new()
                    {
                        Target = "effect.damage.main",
                        Operation = "AddEffect",
                        Value = 1,
                        Effect = new AbilityEffectSpec
                        {
                            Id = "effect.damage.evolved",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 4
                        }
                    }
                ]
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = essence.Id
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var repository = new FakeLegacyDefinitionRepository([ability], [essence]);
        var friendlyCharacter = CreateSourceCharacter("Add Effect Friendly");
        var hostileCharacter = CreateSourceCharacter("Add Effect Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, essence.Id);
        friendlyCombatant.EquippedEssences.Single().IsEvolved = true;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        IncreaseMaxHealth(friendlyCombatant, 2_000);
        IncreaseMaxHealth(hostileCombatant, 2_000);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider, repository);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 10);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.evolved" && x.EventType == EventType.Damage && x.Magnitude == 4);
        Assert.Contains(result.EventLog, x => x.Source == "Add Effect Strike" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_essence_ascension_scaling_at_runtime()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.ascended_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Ascended Strike",
            OwningEssenceId = "essence.test.ascended",
            CooldownTicks = 1,
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100
                }
            ]
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = "essence.test.ascended"
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var friendlyCharacter = CreateSourceCharacter("Ascended Friendly");
        var hostileCharacter = CreateSourceCharacter("Ascended Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.test.ascended");
        friendlyCombatant.EquippedEssences.Single().AscensionTier = 2;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 124);
    }

    [Fact]
    public async Task Combat_engine_executor_scales_evolved_added_effects_with_ascension()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.ascended_add_effect_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Ascended Add Effect Strike",
            OwningEssenceId = "essence.test.ascended_add_effect",
            CooldownTicks = 1,
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100
                }
            ]
        };
        var essence = new EssenceDefinition
        {
            Id = "essence.test.ascended_add_effect",
            ActiveAbilityId = ability.Id,
            Evolution = new EssenceEvolutionDefinition
            {
                ActiveAbilityModifiers =
                [
                    new()
                    {
                        Target = "effect.damage.main",
                        Operation = "AddEffect",
                        Effect = new AbilityEffectSpec
                        {
                            Id = "effect.damage.evolved",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 50
                        }
                    }
                ]
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = essence.Id
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var repository = new FakeLegacyDefinitionRepository([ability], [essence]);
        var friendlyCharacter = CreateSourceCharacter("Ascended Add Effect Friendly");
        var hostileCharacter = CreateSourceCharacter("Ascended Add Effect Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, essence.Id);
        friendlyCombatant.EquippedEssences.Single().IsEvolved = true;
        friendlyCombatant.EquippedEssences.Single().AscensionTier = 1;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider, repository);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 112);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.evolved" && x.EventType == EventType.Damage && x.Magnitude == 56);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_temporary_ability_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.temporary_modifier_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Temporary Modifier Strike",
            OwningEssenceId = "essence.test.temporary_modifier",
            CooldownTicks = 700,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = "essence.test.temporary_modifier"
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var friendlyCharacter = CreateSourceCharacter("Temporary Modifier Friendly");
        var hostileCharacter = CreateSourceCharacter("Temporary Modifier Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.test.temporary_modifier");
        friendlyCombatant.TemporaryAbilityModifiers.Add(new EssenceAbilityModifierDefinition
        {
            Target = "effect.damage.main",
            Operation = "AddMultiplier",
            Value = 0.5
        });
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        IncreaseMaxHealth(friendlyCombatant, 2_000);
        IncreaseMaxHealth(hostileCombatant, 2_000);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 15);
        Assert.Contains(result.EventLog, x => x.Source == "Temporary Modifier Strike" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public async Task Combat_engine_executor_illusion_fox_passive_retaliates_when_holder_is_attacked()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var allyCharacter = CreateSourceCharacter("Fox Ally");
        var foxCharacter = CreateSourceCharacter("Illusion Fox Holder");
        var hostileCharacter = CreateSourceCharacter("Hostile Attacker");
        var allyCombatant = CreateCombatEntity("ally-slot", allyCharacter);
        var foxCombatant = CreateCombatEntity("fox-slot", foxCharacter, "essence.illusion_fox");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("fox-slot", foxCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("ally-slot", allyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(allyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [
                new CombatRuntimeParticipant(plan.FriendlyParticipants[0], foxCharacter, foxCombatant),
                new CombatRuntimeParticipant(plan.FriendlyParticipants[1], allyCharacter, allyCombatant)
            ],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var foxfire = result.EventLog.First(x =>
            x.Source == "effect.foxfire.damage"
            && x.ActorId == "fox-slot"
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);

        Assert.Contains(result.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.ActorId == "fox-slot"
            && x.TargetId == "fox-slot"
            && x.EventType == EventType.StatusEffect);
        Assert.InRange(foxfire.Magnitude, 14, 22);
        Assert.Contains(result.EventLog, x =>
            x.Source == "Basic Attack"
            && x.ActorId == "hostile-slot"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == foxfire.Timestamp);
        Assert.Contains(result.EventLog, x =>
            x.Source == "Basic Attack"
            && x.ActorId == "hostile-slot"
            && x.TargetId == "fox-slot"
            && x.EventType == EventType.Damage
            && x.Timestamp == foxfire.Timestamp);
    }

    [Fact]
    public async Task Combat_engine_executor_summons_temporary_combatant_that_can_act_and_expire()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.shadow_imp"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var summonLog = result.EventLog.First(x =>
            x.Source == "effect.creature.shadow_imp.shadow_image.summon"
            && x.ActorId == "friendly-slot"
            && x.EventType == EventType.Summon);
        var summonId = summonLog.TargetId;

        Assert.NotNull(summonLog.CombatEntity);
        Assert.Equal("Creature Shadow Image", summonLog.CombatEntity!.Name);
        Assert.Equal("shadow_image", summonLog.CombatEntity.ImagePath);
        Assert.True(provider.GetCatalog().SummonsById.ContainsKey("creatureShadowImage"));
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "Shadow Strike"
            && x.EventType == EventType.AbilityUse);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "effect.shadow_image.shadow_strike.damage"
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "Basic Attack"
            && x.EventType == EventType.AbilityUse);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == "friendly-slot"
            && x.TargetId == summonId
            && x.EventType == EventType.SummonExpired);
    }

    [Fact]
    public void Engine_supports_summoned_and_non_summoned_ally_target_selectors()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.buff.summons",
                    Kind = AbilitySpecKind.Active,
                    Name = "Buff Summons",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.buff.summons",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.SummonedAllies,
                            BaseValue = 11
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.buff.non.summons",
                    Kind = AbilitySpecKind.Active,
                    Name = "Buff Non-Summons",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.buff.non.summons",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.NonSummonedAllies,
                            BaseValue = 7
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.damage.enemy.summons",
                    Kind = AbilitySpecKind.Active,
                    Name = "Damage Enemy Summons",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.damage.enemy.summons",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.SummonedEnemies,
                            BaseValue = 13,
                            CritEligibility = CritEligibility.Disallowed
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var summon = new RuntimeCombatant(
            "summon",
            "Summon",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 50,
                [AttributeType.Power] = 0
            },
            [],
            ["Summoned"],
            isSummoned: true,
            summonDurationTicks: 100,
            summonOwner: friendly);
        var hostileSummon = new RuntimeCombatant(
            "hostile-summon",
            "Hostile Summon",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 50,
                [AttributeType.Power] = 0
            },
            [],
            ["Summoned"],
            isSummoned: true,
            summonDurationTicks: 100,
            summonOwner: hostile);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly, ally, summon], [hostile, hostileSummon]);

        Assert.Equal(11, summon.Barrier);
        Assert.Equal(7, friendly.Barrier);
        Assert.Equal(7, ally.Barrier);
        Assert.Equal(37, hostileSummon.Health);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.buff.summons" && x.TargetId == "friendly");
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.buff.non.summons" && x.TargetId == "summon");
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.damage.enemy.summons"
            && x.TargetId == "hostile-summon");
        Assert.DoesNotContain(result.EventLog, x =>
            x.Source == "effect.damage.enemy.summons"
            && x.TargetId == "hostile");
    }

    [Fact]
    public void Engine_starts_summons_ready()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.ready",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Ready",
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "effect.summon.ready",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "readySummon",
                    DurationTicks = 100
                }
            ]
        };
        var strikeAbility = new AbilitySpec
        {
            Id = "ability.summon.ready.strike",
            Kind = AbilitySpecKind.Active,
            Name = "Ready Strike",
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "effect.summon.ready.strike",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 5,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        };
        var compiledAbilities =
            AbilityCompiler.CompileAbilities([summonAbility, strikeAbility]);
        var compiledSummons = AbilityCompiler.CompileSummons(
            [
                new SummonSpec
                {
                    Id = "readySummon",
                    Name = "Ready Summon",
                    DurationTicks = 100,
                    MaxActive = 1,
                    AbilityIds = [strikeAbility.Id],
                    Attributes =
                    [
                        new() { Attribute = AttributeType.MaxHealth, BaseValue = 20, MinimumValue = 1 },
                        new() { Attribute = AttributeType.Power, BaseValue = 1 }
                    ]
                }
            ]);

        static (RuntimeCombatant Friendly, RuntimeCombatant Hostile) CreatePair(
            IReadOnlyDictionary<string, CompiledAbility> abilities) =>
            (
                CreateCombatant(
                    "friendly",
                    CombatTeam.Friendly,
                    [abilities["ability.summon.ready"]]),
                CreateCombatant("hostile", CombatTeam.Hostile, []));

        var defaultPair = CreatePair(compiledAbilities);
        var defaultEngine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                MaxTicks: 2,
                BasicAttackIntervalTicks: 1000,
                StartActiveAbilitiesOnCooldown: true));
        var defaultResult = defaultEngine.Run(
            [defaultPair.Friendly],
            [defaultPair.Hostile]);
        var summon = Assert.Single(
            defaultResult.EventLog,
            x => x.EventType == EventType.Summon);

        Assert.Equal(0, summon.Timestamp);
        Assert.Contains(defaultResult.EventLog, x =>
            x.ActorId == summon.TargetId
            && x.Source == "Ready Strike"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == 1);
        Assert.Contains(defaultResult.EventLog, x =>
            x.ActorId == summon.TargetId
            && x.Source == "Basic Attack"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == 1);
    }

    [Fact]
    public void Engine_enforces_summon_template_active_cap()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.cap",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Cap",
            Effects =
            [
                new()
                {
                    Id = "effect.summon.cap",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "cappedSummon",
                    DurationTicks = 100
                }
            ]
        };
        var compiledAbilities = AbilityCompiler.CompileAbilities([summonAbility]);
        var compiledSummons = AbilityCompiler.CompileSummons(
            [
                new SummonSpec
                {
                    Id = "cappedSummon",
                    Name = "Capped Summon",
                    MaxActive = 1,
                    Attributes =
                    [
                        new() { Attribute = AttributeType.MaxHealth, BaseValue = 20, MinimumValue = 1 },
                        new() { Attribute = AttributeType.Power, BaseValue = 0 }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.summon.cap" && x.EventType == EventType.Summon);
    }

    [Fact]
    public void Engine_expires_owned_summons_when_summoner_dies()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.owner.cleanup",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Cleanup",
            Effects =
            [
                new()
                {
                    Id = "effect.summon.cleanup",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "cleanupSummon",
                    DurationTicks = 100
                }
            ]
        };
        var killAbility = new AbilitySpec
        {
            Id = "ability.kill.owner",
            Kind = AbilitySpecKind.Active,
            Name = "Kill Owner",
            Effects =
            [
                new()
                {
                    Id = "effect.kill.owner",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 500
                }
            ]
        };
        var compiledAbilities = AbilityCompiler.CompileAbilities([summonAbility, killAbility]);
        var compiledSummons = AbilityCompiler.CompileSummons(
            [
                new SummonSpec
                {
                    Id = "cleanupSummon",
                    Name = "Cleanup Summon",
                    MaxActive = 1,
                    Attributes =
                    [
                        new() { Attribute = AttributeType.MaxHealth, BaseValue = 20, MinimumValue = 1 },
                        new() { Attribute = AttributeType.Power, BaseValue = 0 }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [compiledAbilities["ability.summon.owner.cleanup"]], maxHealth: 50);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [compiledAbilities["ability.kill.owner"]]);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);
        var summonLog = Assert.Single(result.EventLog, x => x.Source == "effect.summon.cleanup" && x.EventType == EventType.Summon);
        var summonId = summonLog.TargetId;

        Assert.Contains(result.EventLog, x => x.Source == "effect.kill.owner" && x.TargetId == "friendly" && x.EventType == EventType.Death);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == "friendly"
            && x.TargetId == summonId
            && x.EventType == EventType.SummonExpired
            && x.Details.Contains("owner death", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Combat_engine_executor_attributes_applied_status_damage_to_parent_ability()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.goblin"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly-slot");
        var shivJab = Assert.Single(friendlyStats.Abilities, x => x.Name == "Shiv Jab");

        Assert.True(shivJab.Uses > 0);
        Assert.True(shivJab.TotalDamage > 0);
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.creature.goblin.shiv_jab.damage"
            && x.StatsSource == "Shiv Jab"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.Source == "condition.bleed"
            && x.StatsSource == "Shiv Jab"
            && x.EventType == EventType.Damage);
    }

    [Fact]
    public async Task Combat_engine_executor_counts_multi_effect_passive_trigger_as_one_proc()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.goblin_warrior"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly-slot");
        var relentless = Assert.Single(friendlyStats.Abilities, x => x.Name == "Relentless");

        Assert.Equal(1, relentless.Uses);
        Assert.Single(result.EventLog, x =>
            x.ActorId == "friendly-slot"
            && x.StatsSource == "Relentless"
            && x.EventType == EventType.Buff);
    }

    [Fact]
    public async Task Combat_engine_executor_syncs_final_runtime_state_to_combat_entities()
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, CombatMode.Pvp);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var lastHostileSnapshot = result.EventLog
            .Where(x => x.TargetId == "hostile-slot" && x.CombatEntity is not null)
            .Select(x => x.CombatEntity!)
            .Last();

        Assert.Equal(lastHostileSnapshot.Health, runtime.HostileParticipants.Single().Combatant.GetCurrentHealthValue());
        Assert.Equal(lastHostileSnapshot.Barrier, runtime.HostileParticipants.Single().Combatant.GetCurrentBarrierValue());
    }

    [Fact]
    public async Task Combat_engine_simulation_returns_final_team_health_snapshots()
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, CombatMode.Dungeon);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteSimulationAsync(
            runtime,
            new CombatSimulationOptions(1337, 6000),
            CancellationToken.None);

        var friendly = Assert.Single(result.PlayerTeam);
        var hostile = Assert.Single(result.EnemyTeam);
        Assert.Equal("friendly-slot", friendly.Id);
        Assert.Equal("hostile-slot", hostile.Id);
        Assert.True(friendly.MaxHealth > 0);
        Assert.True(hostile.MaxHealth > 0);
        Assert.InRange(friendly.Health, 0, friendly.MaxHealth);
        Assert.InRange(hostile.Health, 0, hostile.MaxHealth);
    }

    [Theory]
    [InlineData(CombatMode.Idle)]
    [InlineData(CombatMode.Dungeon)]
    [InlineData(CombatMode.Pvp)]
    [InlineData(CombatMode.Raid)]
    public async Task Combat_engine_executor_runs_outer_runtime_shapes(CombatMode mode)
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, mode);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Equal(mode, runtime.Plan.Mode);
        Assert.Equal(mode, runtime.Plan.SourceContext.Mode);
        Assert.Equal(runtime.Plan.StartsAt, result.StartedAt);
        Assert.True(result.Duration > 0);
        Assert.NotEmpty(result.EventLog);
        Assert.NotEmpty(result.EntityStats);
        Assert.Contains(result.EventLog, x => x.Source == "effect.creature.goblin.shiv_jab.damage" && x.EventType == EventType.Damage);
    }

    [Fact]
    public void Ability_catalog_diagnostics_runs_training_encounter()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var diagnostics = new AbilityCatalogDiagnostics(provider);

        var report = diagnostics.RunTrainingEncounter();

        Assert.True(report.AbilityCount >= 3);
        Assert.True(report.StatusCount >= 2);
        Assert.True(report.SummonCount >= 1);
        Assert.True(report.IndexedSummonTags >= 1);
        Assert.True(report.TimedSummonCount >= 1);
        Assert.True(report.SummonAbilityReferenceCount >= 1);
        Assert.Contains(report.Summons, x =>
            x.Id == "shadowImage"
            && x.HasTimedDuration
            && x.ExpiresOnOwnerDeath
            && x.AbilityIds.Contains("ability.summon.shadow_image.shadow_strike"));
        Assert.True(report.DirectDamageObserved);
        Assert.True(report.BarrierObserved);
        Assert.True(report.DamageOverTimeObserved);
        Assert.True(report.ReflectObserved);
        Assert.Empty(report.Failures);
    }

    [Fact]
    public void Ability_catalog_coverage_reports_missing_and_ambiguous_essence_slots()
    {
        var essences = new List<EssenceDefinition>
        {
            new()
            {
                Id = "essence.covered",
                ActiveAbilityId = "ability.covered.active",
                PassiveAbilityId = "ability.covered.passive"
            },
            new()
            {
                Id = "essence.missing",
                ActiveAbilityId = "legacy.missing.active",
                PassiveAbilityId = "legacy.missing.passive"
            },
            new()
            {
                Id = "essence.ambiguous",
                ActiveAbilityId = "legacy.ambiguous.active",
                PassiveAbilityId = "legacy.ambiguous.passive"
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [
                CreateOwnedAbility("ability.covered.active", "essence.covered", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.covered.passive", "essence.covered", AbilitySpecKind.Passive),
                CreateOwnedAbility("ability.missing.active", "essence.missing", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.ambiguous.active.one", "essence.ambiguous", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.ambiguous.active.two", "essence.ambiguous", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.unowned", "essence.not.real", AbilitySpecKind.Active)
            ],
            []);
        var analyzer = new AbilityCatalogCoverageAnalyzer(
            new FakeLegacyDefinitionRepository([], essences),
            new FakeAbilityCatalogProvider(catalog));

        var report = analyzer.Analyze();

        Assert.False(report.IsComplete);
        Assert.Equal(3, report.EssenceCount);
        Assert.Equal(6, report.RequiredSlotCount);
        Assert.Equal(3, report.CoveredSlotCount);
        Assert.Equal(2, report.CurrentReferenceCoveredSlotCount);
        Assert.Equal(3, report.RuntimeLoadoutChecks.Count);
        Assert.Contains(report.RuntimeLoadoutChecks, x => x.EssenceId == "essence.missing" && !x.IsReady);
        Assert.Contains(report.Gaps, x => x.EssenceId == "essence.missing" && x.Slot == "Passive" && x.Reason.Contains("No Passive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Gaps, x => x.EssenceId == "essence.ambiguous" && x.Slot == "Active" && x.Reason.Contains("Multiple Active", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.UnownedAbilityIds, x => x == "ability.unowned");
    }

    private static CombatResult RunBattle(
        IReadOnlyList<AbilitySpec> friendlyAbilities,
        IReadOnlyList<StatusSpec> statuses,
        int maxTicks,
        out RuntimeCombatant friendly,
        out RuntimeCombatant hostile,
        int seed = 1337)
    {
        var compiledAbilities = AbilityCompiler.CompileAbilities(friendlyAbilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(statuses);
        friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);

        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(maxTicks, RandomSeed: seed));
        return engine.Run([friendly], [hostile]);
    }

    private static CombatEncounterRuntime CreateTrainingEncounterRuntime(
        out Character friendlyCharacter,
        out Character hostileCharacter,
        CombatMode mode = CombatMode.Idle,
        Guid? encounterId = null)
    {
        friendlyCharacter = CreateSourceCharacter("Executor Friendly");
        hostileCharacter = CreateSourceCharacter("Executor Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.goblin");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            encounterId ?? Guid.NewGuid(),
            mode,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            CreateSourceContext(mode, friendlyCharacter.Id, hostileCharacter.Id));

        return new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
    }

    private static CombatEncounterRuntime CreateRealEssenceEncounterRuntime(
        CombatMode mode,
        IReadOnlyList<string> friendlyEssenceIds,
        IReadOnlyList<string> hostileEssenceIds,
        out Character friendlyCharacter,
        out Character hostileCharacter)
    {
        friendlyCharacter = CreateSourceCharacter("Real Friendly");
        hostileCharacter = CreateSourceCharacter("Real Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, [.. friendlyEssenceIds]);
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter, [.. hostileEssenceIds]);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            mode,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            CreateSourceContext(mode, friendlyCharacter.Id, hostileCharacter.Id));

        return new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
    }

    private static CombatEncounterSourceContext CreateSourceContext(
        CombatMode mode,
        Guid friendlyCharacterId,
        Guid hostileCharacterId) =>
        mode switch
        {
            CombatMode.Dungeon => new DungeonEncounterSourceContext(Guid.NewGuid()),
            CombatMode.Pvp => new PvpEncounterSourceContext(Guid.NewGuid(), friendlyCharacterId, hostileCharacterId),
            CombatMode.Raid => new RaidEncounterSourceContext(Guid.NewGuid(), PhaseIndex: 1, StageKey: "test-stage"),
            _ => new IdleEncounterSourceContext(friendlyCharacterId, new Area(), TimeSpan.FromSeconds(1))
        };

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        int maxHealth = 200,
        int dodgeChance = 0) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = 50,
                [AttributeType.CritDamage] = 100,
                [AttributeType.DodgeChance] = dodgeChance,
                [AttributeType.AttackSpeed] = 0
            },
            abilities,
            ["Role.Test"]);

    private static int CountBasicAttacks(CombatResult result, string actorId) =>
        result.EventLog.Count(log =>
            log.EventType == EventType.AbilityUse &&
            log.Source == "Basic Attack" &&
            log.ActorId == actorId);

    private static IReadOnlyDictionary<string, CompiledAbility> CompileCatalogAbilities(
        AbilityCatalog catalog,
        params string[] abilityIds) =>
        AbilityCompiler.CompileAbilities(abilityIds.Select(id => catalog.AbilitiesById[id]));

    private static AbilitySpec CreatePassiveBarrier(
        string id,
        string effectId,
        AbilityTriggerEvent triggerEvent,
        int value) =>
        new()
        {
            Id = id,
            Kind = AbilitySpecKind.Passive,
            Name = id,
            Triggers = [new() { Event = triggerEvent }],
            Effects =
            [
                new()
                {
                    Id = effectId,
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = value
                }
            ]
        };

    private static Character CreateSourceCharacter(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = 10
        };

    private static CombatEntity CreateCombatEntity(
        string runtimeId,
        Character source,
        params string[] equippedEssenceIds)
    {
        FillCombatAttributes(source.BaseCombatAttributes);
        FillCombatAttributes(source.CombatAttributes);

        var combatant = new CombatEntity(source)
        {
            Id = runtimeId,
            Name = source.Name,
            Level = source.Level
        };

        FillCombatAttributes(combatant.BaseCombatAttributes);
        FillCombatAttributes(combatant.CombatAttributes);
        combatant.SyncCurrentHealthToMax();

        foreach (var equippedEssenceId in equippedEssenceIds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            combatant.EquippedEssences.Add(new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = source.Id,
                EssenceDefinitionId = equippedEssenceId,
                Level = 1
            });
        }

        return combatant;
    }

    private static void FillCombatAttributes(IDictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = 200;
        attributes[AttributeType.Power] = 50;
        attributes[AttributeType.CritDamage] = 100;
        attributes[AttributeType.AttackSpeed] = 0;
    }

    private static void IncreaseMaxHealth(CombatEntity combatant, float maxHealth)
    {
        combatant.BaseCombatAttributes[AttributeType.MaxHealth] = maxHealth;
        combatant.CombatAttributes[AttributeType.MaxHealth] = maxHealth;
        combatant.SyncCurrentHealthToMax();
    }

    private static AbilitySpec CreateDamageAbility(string id, string tag) =>
        new()
        {
            Id = id,
            Kind = AbilitySpecKind.Active,
            Name = id,
            Tags = [tag],
            Triggers = [new() { Event = AbilityTriggerEvent.OnAbilityUsed }],
            Effects =
            [
                new()
                {
                    Id = "effect.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static StatusSpec CreateBurnStatus() =>
        new()
        {
            Id = "status.burn",
            Name = "Burn",
            StackingPolicy = AbilityStatusStackingPolicy.Stack,
            MaxStacks = 5,
            DurationTicks = 20,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["effect.burn.dot"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.burn.dot",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventTarget,
                    BaseValue = 3,
                    DurationTicks = 9,
                    IntervalTicks = 3,
                    AttackType = AttackType.DamageOverTime,
                    DamageType = DamageType.Burn
                }
            ]
        };

    private static StatusSpec CreateThornsStatus() =>
        new()
        {
            Id = "status.thorns",
            Name = "Thorns",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 100,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnDamaged,
                    EffectIds = ["effect.thorns.reflect"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.thorns.reflect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventTarget,
                    BaseValue = 6,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static StatusSpec CreateStunStatus() =>
        new()
        {
            Id = "status.stunned",
            Name = "Stunned",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 30,
            Tags = ["Control.Stun"]
        };

    private static StatusSpec CreateEmptyStatus(
        string id,
        AbilityStatusStackingPolicy stackingPolicy,
        int maxStacks,
        int durationTicks) =>
        new()
        {
            Id = id,
            Name = id,
            StackingPolicy = stackingPolicy,
            MaxStacks = maxStacks,
            DurationTicks = durationTicks
        };

    private static StatusSpec CreateTimedPowerBuffStatus() =>
        new()
        {
            Id = "status.power.buff",
            Name = "Power Buff",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 10,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["effect.status.power.buff"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.status.power.buff",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.Power,
                    BaseValue = 20,
                    DurationTicks = 2
                }
            ]
        };

    private static StatusSpec CreateTimedAttackSpeedStatus(
        string statusId,
        string effectId,
        int amount,
        int durationTicks) =>
        new()
        {
            Id = statusId,
            Name = statusId,
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = durationTicks,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = [effectId]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = effectId,
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.AttackSpeed,
                    BaseValue = amount,
                    DurationTicks = durationTicks
                }
            ]
        };

    private static AbilityEffectSpec CreateApplyStatusEffect(
        string id,
        string statusId,
        AbilityTargetSelector target = AbilityTargetSelector.CurrentTarget) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.ApplyStatus,
            Target = target,
            StatusId = statusId,
            BaseValue = 1
        };

    private static AbilitySpec CreateOwnedAbility(string id, string owningEssenceId, AbilitySpecKind kind) =>
        new()
        {
            Id = id,
            Name = id,
            OwningEssenceId = owningEssenceId,
            Kind = kind,
            Effects =
            [
                new()
                {
                    Id = "effect.noop",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 1
                }
            ]
        };

    private static IConfiguration CreateConfig(bool? useV2Engine = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data",
                ["Combat:UseV2Engine"] = useV2Engine?.ToString()
            })
            .Build();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var apiPath in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                var dataPath = Path.Combine(apiPath, "Data");
                var abilityCandidate = Path.Combine(dataPath, "combat", "abilities.json");
                var statusCandidate = Path.Combine(dataPath, "combat", "statuses.json");
                var summonCandidate = Path.Combine(dataPath, "combat", "summons.json");
                if (File.Exists(abilityCandidate) && File.Exists(statusCandidate) && File.Exists(summonCandidate))
                    return apiPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/combat/abilities.json, statuses.json, and summons.json from test output directory.");
    }

    private sealed class FakeLegacyDefinitionRepository(
        IReadOnlyList<AbilitySpec> abilities,
        IReadOnlyList<EssenceDefinition>? essences = null) : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => essences ?? [];
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => abilities;
        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            essences?.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public AbilitySpec? GetAbilityById(string abilityId) =>
            abilities.FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeAbilityCatalogProvider(AbilityCatalog catalog) : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => catalog;
    }

}
