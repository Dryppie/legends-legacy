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
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Interfaces.Combat.Resolution;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AbilitySystemTests
{
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
        Assert.Equal(5, report.CandidateTeamCount);
        Assert.NotEmpty(report.RankedCombinations);
        Assert.Equal(40, report.RankedCombinations.Sum(combination => combination.Battles));
        Assert.All(report.RankedCombinations, combination =>
        {
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
            [new AbilityBalanceParticipantLoadout(["essence.vampire_bat"])]);
        var second = new AbilityBalanceTeamLoadout(
            [new AbilityBalanceParticipantLoadout(["essence.raven"])]);

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
        Assert.Equal(2, report.RankedCombinations.Count);
        Assert.All(report.RankedCombinations, combination => Assert.Equal(6, combination.Battles));
    }

    [Fact]
    public void Balance_simulator_uses_the_selected_canonical_equipment_build()
    {
        var configuration = CreateConfig();
        var contentRoot = FindApiContentRoot();
        var jsonOptions = CreateJsonOptions();
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var balance = Options.Create(new CraftingBalanceOptions());
        var canonicalBuilds = new CanonicalEquipmentBuildFactory(
            new JsonCraftingDefinitionProvider(configuration, contentRoot, jsonOptions),
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        var simulator = new AbilityBalanceSimulator(
            new JsonAbilityCatalogProvider(configuration, contentRoot, jsonOptions),
            essenceDefinitions,
            canonicalBuilds);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 2,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: 789,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams: null,
            EquipmentTier: 3,
            EquipmentRarity: "Rare",
            EquipmentProfile: "Offense"));

        Assert.Equal(3, report.EquipmentTier);
        Assert.Equal("Rare", report.EquipmentRarity);
        Assert.Equal("Offense", report.EquipmentProfile);
        Assert.True(report.ParticipantAttributes.Count > 3);
        Assert.True(report.ParticipantAttributes[AttributeType.MaxHealth.ToString()] > 200);
    }

    [Fact]
    public void Log_free_engine_mode_preserves_outcome_duration_and_damage_totals()
    {
        var ability = AbilityCompiler.CompileAbility(CreateDamageAbility("ability.log-free", "Test"));

        CombatResult Run(bool captureEventLog)
        {
            var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [ability]);
            var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(
                    MaxTicks: 20,
                    RandomSeed: 42,
                    CaptureEventLog: captureEventLog));
            return engine.Run([friendly], [hostile]);
        }

        var detailed = Run(captureEventLog: true);
        var logFree = Run(captureEventLog: false);

        Assert.NotEmpty(detailed.EventLog);
        Assert.Empty(logFree.EventLog);
        Assert.Equal(detailed.Outcome, logFree.Outcome);
        Assert.Equal(detailed.Duration, logFree.Duration);
        Assert.Equal(
            detailed.EntityStats.Sum(stats => stats.DamageDone),
            logFree.EntityStats.Sum(stats => stats.DamageDone));
        Assert.Equal(
            detailed.EntityStats.Sum(stats => stats.DamageTaken),
            logFree.EntityStats.Sum(stats => stats.DamageTaken));
    }

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

    [Theory]
    [InlineData(AbilityEffectOperation.Damage)]
    [InlineData(AbilityEffectOperation.Heal)]
    [InlineData(AbilityEffectOperation.GrantBarrier)]
    public void Catalog_rejects_progression_magnitudes_without_a_scaling_source(
        AbilityEffectOperation operation)
    {
        var ability = new AbilitySpec
        {
            Id = $"ability.unscaled.{operation}",
            Kind = AbilitySpecKind.Active,
            Name = "Unscaled magnitude",
            Effects =
            [
                new()
                {
                    Id = "effect.unscaled",
                    Operation = operation,
                    Target = operation == AbilityEffectOperation.Damage
                        ? AbilityTargetSelector.CurrentTarget
                        : AbilityTargetSelector.Self
                }
            ]
        };

        var validation = AbilityCatalogValidator.Validate([ability], []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error =>
            error.Contains("requires a positive", StringComparison.OrdinalIgnoreCase)
            && error.Contains("scaling source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_accepts_event_scaled_progression_magnitudes()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.event-scaled",
            Kind = AbilitySpecKind.Passive,
            Name = "Event scaled",
            Effects =
            [
                new()
                {
                    Id = "effect.event-scaled",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventTarget,
                    EventMagnitudeCoefficient = 0.1f
                }
            ]
        };

        var validation = AbilityCatalogValidator.Validate([ability], []);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
    }

    [Fact]
    public void Catalog_requires_combat_summons_to_have_health_and_inherit_power()
    {
        var summon = new SummonSpec
        {
            Id = "summon.unscaled",
            Name = "Unscaled summon",
            Attributes =
            [
                new() { Attribute = AttributeType.MaxHealth, BaseValue = 0 },
                new() { Attribute = AttributeType.Power, BaseValue = 5 }
            ]
        };

        var validation = AbilityCatalogValidator.Validate([], [], summons: [summon]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("durability", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, error => error.Contains("basic-attacking", StringComparison.OrdinalIgnoreCase));
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
    public void Engine_spawns_ten_continuous_hostile_waves_without_resetting_friendly_cooldowns()
    {
        var ability = CreateDamageAbility("ability.wave.strike", "Family.Test");
        ability.CooldownTicks = 3;
        var compiled = AbilityCompiler.CompileAbility(ability);
        var friendly = CreateCombatant(
            "friendly",
            CombatTeam.Friendly,
            [compiled],
            maxHealth: 10_000,
            canBasicAttack: false);
        var firstWave = CreateCombatant("wave-1", CombatTeam.Hostile, [], maxHealth: 1);
        var reinforcementWaves = Enumerable.Range(2, 9)
            .Select(wave => (IReadOnlyList<RuntimeCombatant>)[
                CreateCombatant($"wave-{wave}", CombatTeam.Hostile, [], maxHealth: 1)
            ])
            .ToArray();
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 30, BasicAttackIntervalTicks: 1));
        var checkpoints = new List<CombatCheckpoint>();

        var result = engine.Run(
            [friendly],
            [firstWave],
            checkpointObserver: checkpoints.Add,
            checkpointIntervalTicks: 10,
            hostileReinforcementWaves: reinforcementWaves);

        Assert.Equal(BattleOutcome.Victory, result.Outcome);
        Assert.Equal(28, result.Duration);
        Assert.True(friendly.Health < 9_900, $"Expected damage to accumulate across waves, but Health was {friendly.Health}.");
        Assert.Equal(
            Enumerable.Range(0, 10).Select(index => index * 3),
            result.EventLog
                .Where(x => x.Source == ability.Name && x.EventType == EventType.AbilityUse)
                .Select(x => x.Timestamp));
        Assert.All(
            Enumerable.Range(1, 10),
            wave => Assert.Contains(result.EntityStats, x => x.EntityId == $"wave-{wave}"));
        Assert.All(
            Enumerable.Range(2, 9),
            wave => Assert.Contains(
                checkpoints,
                checkpoint => checkpoint.Hostile.Any(entity =>
                    entity.Id == $"wave-{wave}" && entity.Health == entity.MaxHealth)));
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
    public void Engine_regenerates_health_every_five_seconds_from_the_direct_stat()
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        friendly.Attributes[AttributeType.HealthRegeneration] = 3;
        friendly.AdjustHealth(-20);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 100, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(186, friendly.Health);

        var regeneration = result.EventLog
            .Where(x => x.EventType == EventType.HealthRegeneration && x.TargetId == friendly.Id)
            .ToList();

        Assert.Equal([49, 99], regeneration.Select(x => x.Timestamp));
        Assert.All(regeneration, x => Assert.Equal(3, x.Magnitude));
        var stats = result.EntityStats.Single(x => x.EntityId == friendly.Id);
        Assert.Equal(6, stats.HealthRegenerated);
        Assert.Equal(6, stats.HealthRegenerationPotential);
        Assert.Equal(0, stats.HealthRegenerationOverhealed);
        Assert.Equal(2, stats.HealthRegenerationPulses);
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
    public void Blood_feeders_checks_the_enemy_hit_instead_of_its_self_heal_target()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var bloodFeeders = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.bog_mite.blood_feeders"]);
        var strike = AbilityCompiler.CompileAbility(
            CreateFixedDamageAbility("ability.test.bog-mite-strike", "effect.test.bog-mite-strike", 100));

        CombatResult Run(bool poisonFriendly, bool poisonHostile)
        {
            var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [bloodFeeders, strike]);
            var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 1_000);
            friendly.SetHealth(100);
            if (poisonFriendly)
                AddStandardCondition(friendly, hostile, StandardConditionType.Poison);
            if (poisonHostile)
                AddStandardCondition(hostile, friendly, StandardConditionType.Poison);

            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));
            return engine.Run([friendly], [hostile]);
        }

        var poisonedEnemy = Run(poisonFriendly: false, poisonHostile: true);
        var poisonedSelf = Run(poisonFriendly: true, poisonHostile: false);

        Assert.Contains(poisonedEnemy.EventLog, item =>
            item.Source == "effect.creature.bog_mite.blood_feeders.heal"
            && item.EventType == EventType.Heal
            && item.Magnitude == 5);
        Assert.Equal(
            5,
            poisonedEnemy.EntityStats
                .Single(item => item.EntityId == "friendly")
                .Abilities.Single(item => item.Name == "Blood Feeders")
                .TotalHealing);
        Assert.DoesNotContain(poisonedSelf.EventLog, item =>
            item.Source == "effect.creature.bog_mite.blood_feeders.heal");
    }

    [Fact]
    public void Rotfly_host_heals_when_an_ally_kills_a_decayed_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var rotflyHost = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.rotfly_toad.rotfly_host"]);
        var strike = AbilityCompiler.CompileAbility(
            CreateFixedDamageAbility("ability.test.rotfly-strike", "effect.test.rotfly-strike", 100));
        var rotflyOwner = CreateCombatant("rotfly-owner", CombatTeam.Friendly, [rotflyHost]);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, [strike]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 50);
        rotflyOwner.SetHealth(100);
        AddStandardCondition(hostile, rotflyOwner, StandardConditionType.Decay);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([rotflyOwner, ally], [hostile]);

        Assert.Contains(result.EventLog, item =>
            item.ActorId == rotflyOwner.Id
            && item.Source == "effect.creature.rotfly_toad.rotfly_host.heal"
            && item.EventType == EventType.Heal
            && item.Magnitude == 16);
        Assert.Equal(116, rotflyOwner.Health);
        Assert.Contains(AbilityTriggerEvent.OnEnemyDeath, rotflyHost.TriggersByEvent.Keys);
    }

    [Fact]
    public void On_enemy_death_notifies_living_opponents_but_not_the_victims_allies()
    {
        var listener = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.enemy-death-listener",
            Kind = AbilitySpecKind.Passive,
            Name = "Enemy Death Listener",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnEnemyDeath,
                    EffectIds = ["effect.test.enemy-death-listener"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.test.enemy-death-listener",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 7
                }
            ]
        });
        var executeSpec = CreateFixedDamageAbility(
            "ability.test.enemy-death-execute",
            "effect.test.enemy-death-execute",
            100);
        executeSpec.Effects.Single().Target = AbilityTargetSelector.LowestCurrentHealthEnemy;
        var execute = AbilityCompiler.CompileAbility(executeSpec);
        var friendlyObserver = CreateCombatant("friendly-observer", CombatTeam.Friendly, [listener]);
        var attacker = CreateCombatant("attacker", CombatTeam.Friendly, [execute]);
        var victim = CreateCombatant("victim", CombatTeam.Hostile, [], maxHealth: 5);
        var hostileObserver = CreateCombatant("hostile-observer", CombatTeam.Hostile, [listener]);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([friendlyObserver, attacker], [victim, hostileObserver]);

        Assert.Equal(7, friendlyObserver.Barrier);
        Assert.Equal(0, hostileObserver.Barrier);
    }

    [Fact]
    public void Effect_condition_target_still_resolves_to_the_effect_recipient()
    {
        var conditionalBarrier = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.effect-target-condition",
            Kind = AbilitySpecKind.Passive,
            Name = "Effect Target Condition",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnHit,
                    EffectIds = ["effect.test.effect-target-condition"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.test.effect-target-condition",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 7,
                    Conditions =
                    [
                        new()
                        {
                            Type = AbilityConditionType.HasCondition,
                            Subject = AbilityConditionSubject.Target,
                            Condition = StandardConditionType.Recovery
                        }
                    ]
                }
            ]
        });
        var strike = AbilityCompiler.CompileAbility(
            CreateFixedDamageAbility("ability.test.effect-target-strike", "effect.test.effect-target-strike", 10));
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [conditionalBarrier, strike]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        AddStandardCondition(friendly, friendly, StandardConditionType.Recovery);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([friendly], [hostile]);

        Assert.Equal(7, friendly.Barrier);
    }

    [Fact]
    public void Engine_weights_taunting_targets_for_basic_attacks()
    {
        var front = CreateCombatant("front", CombatTeam.Friendly, [], maxHealth: 2_000);
        var taunter = CreateCombatant("taunter", CombatTeam.Friendly, [], maxHealth: 2_000);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 2_000);
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
            new FastCombatEngineOptions(MaxTicks: 20, BasicAttackIntervalTicks: 1));

        var result = engine.Run([front, taunter], [hostile]);
        var hostileAttacks = result.EventLog
            .Where(x =>
                x.ActorId == "hostile"
                && x.Source == "Basic Attack"
                && x.EventType == EventType.Damage)
            .ToList();
        var attacksAgainstFront = hostileAttacks.Count(x => x.TargetId == "front");
        var attacksAgainstTaunter = hostileAttacks.Count(x => x.TargetId == "taunter");

        Assert.Equal(20, hostileAttacks.Count);
        Assert.True(attacksAgainstTaunter > attacksAgainstFront);
    }

    [Fact]
    public void Engine_emits_ordered_ten_tick_checkpoints_and_exact_final_frame()
    {
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var checkpoints = new List<CombatCheckpoint>();
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 21, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run(
            [friendly],
            [hostile],
            checkpointObserver: checkpoints.Add,
            checkpointIntervalTicks: 10);

        Assert.Equal(21, result.Duration);
        Assert.Equal([0, 10, 20, 21], checkpoints.Select(x => x.Tick));
        Assert.Equal([0, 1, 2, 3], checkpoints.Select(x => x.Sequence));
        Assert.DoesNotContain(checkpoints.Take(3), x => x.IsFinal);
        Assert.True(checkpoints[^1].IsFinal);
        Assert.Equal((int)friendly.Health, checkpoints[^1].Friendly.Single().Health);
        Assert.Equal((int)hostile.Health, checkpoints[^1].Hostile.Single().Health);
    }

    [Fact]
    public void Checkpoint_capture_does_not_change_deterministic_result()
    {
        static CombatResult Run(bool capture)
        {
            var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
            var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(MaxTicks: 30, BasicAttackIntervalTicks: 3, RandomSeed: 27));
            return capture
                ? engine.Run([friendly], [hostile], checkpointObserver: _ => { }, checkpointIntervalTicks: 10)
                : engine.Run([friendly], [hostile]);
        }

        var baseline = Run(false);
        var checkpointed = Run(true);

        Assert.Equal(baseline.Outcome, checkpointed.Outcome);
        Assert.Equal(baseline.Duration, checkpointed.Duration);
        Assert.Equal(
            baseline.EventLog.Select(x => (x.Timestamp, x.ActorId, x.TargetId, x.EventType, x.Magnitude)),
            checkpointed.EventLog.Select(x => (x.Timestamp, x.ActorId, x.TargetId, x.EventType, x.Magnitude)));
        Assert.Equal(
            baseline.EntityStats.Select(x => (x.EntityId, x.DamageDone, x.DamageTaken, x.HealingDone)),
            checkpointed.EntityStats.Select(x => (x.EntityId, x.DamageDone, x.DamageTaken, x.HealingDone)));
    }

    [Fact]
    public void Tower_checkpoint_capture_keeps_cumulative_stats_without_an_event_log()
    {
        static (CombatResult Result, IReadOnlyList<CombatCheckpoint> Checkpoints) Run(bool captureEventLog)
        {
            var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [], maxHealth: 2_000);
            var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 2_000);
            var checkpoints = new List<CombatCheckpoint>();
            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(
                    MaxTicks: 30,
                    BasicAttackIntervalTicks: 3,
                    RandomSeed: 91,
                    CaptureEventLog: captureEventLog));
            var result = engine.Run(
                [friendly],
                [hostile],
                checkpointObserver: checkpoints.Add,
                checkpointIntervalTicks: 10);
            return (result, checkpoints);
        }

        var detailed = Run(true);
        var compact = Run(false);

        Assert.NotEmpty(detailed.Result.EventLog);
        Assert.Empty(compact.Result.EventLog);
        Assert.Equal(detailed.Result.Outcome, compact.Result.Outcome);
        Assert.Equal(detailed.Result.Duration, compact.Result.Duration);
        Assert.Equal(
            detailed.Result.EntityStats.Select(x =>
                (x.EntityId, x.DamageDone, x.DamageTaken, x.HealingDone, x.BarrierGenerated, x.DamageBlocked)),
            compact.Result.EntityStats.Select(x =>
                (x.EntityId, x.DamageDone, x.DamageTaken, x.HealingDone, x.BarrierGenerated, x.DamageBlocked)));
        Assert.All(compact.Checkpoints, checkpoint => Assert.Empty(checkpoint.Events));
        Assert.Equal(
            compact.Result.EntityStats.Select(x => (x.EntityId, x.DamageDone, x.DamageTaken)),
            compact.Checkpoints[^1].EntityStats.Select(x => (x.EntityId, x.DamageDone, x.DamageTaken)));
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
        var selectedEnemies = result.EventLog
            .Where(x => x.Source == "effect.two.enemies" && x.EventType == EventType.Damage)
            .Select(x => x.TargetId)
            .ToArray();
        Assert.Equal(2, selectedEnemies.Distinct().Count());
        Assert.All(
            selectedEnemies,
            id => Assert.Contains(id, new[] { "hostile-1", "hostile-2", "hostile-3" }));
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "effect.two.allies" && x.EventType == EventType.RestoreBarrier));
        Assert.Single(result.EventLog, x => x.Source == "effect.highest.max.health" && x.TargetId == "high-health-ally");
        Assert.Equal(9, highHealthAlly.Barrier);
    }

    [Fact]
    public void All_allies_includes_the_caster_and_living_allied_summons_only()
    {
        var ability = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.all.allies",
            Kind = AbilitySpecKind.Active,
            Name = "All Allies",
            Effects =
            [
                new()
                {
                    Id = "effect.all.allies",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.AllAllies,
                    BaseValue = 5
                }
            ]
        });
        var caster = CreateCombatant("caster", CombatTeam.Friendly, [ability]);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var deadAlly = CreateCombatant("dead-ally", CombatTeam.Friendly, []);
        deadAlly.SetHealth(0);
        var alliedSummon = CreateOwnedBroodling("allied-summon", caster, 50);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([caster, ally, deadAlly, alliedSummon], [enemy]);

        Assert.Equal(
            ["caster", "ally", "allied-summon"],
            result.EventLog
                .Where(log => log.Source == "effect.all.allies" && log.EventType == EventType.RestoreBarrier)
                .Select(log => log.TargetId)
                .ToArray());
        Assert.Equal(0, deadAlly.Barrier);
        Assert.Equal(0, enemy.Barrier);
    }

    [Fact]
    public void Party_allies_are_isolated_while_all_enemies_still_crosses_parties()
    {
        var partyBarrier = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.party.barrier",
            Kind = AbilitySpecKind.Active,
            Name = "Party Barrier",
            Effects =
            [
                new()
                {
                    Id = "effect.party.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.AllAllies,
                    BaseValue = 5
                }
            ]
        });
        var enemyAreaDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.enemy.area",
            Kind = AbilitySpecKind.Active,
            Name = "Enemy Area Damage",
            Effects =
            [
                new()
                {
                    Id = "effect.enemy.area",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.AllEnemies,
                    BaseValue = 5
                }
            ]
        });
        var caster = CreateCombatant("caster", CombatTeam.Friendly, [partyBarrier], partyNumber: 1);
        var samePartyAlly = CreateCombatant("party-1-ally", CombatTeam.Friendly, [], partyNumber: 1);
        var otherPartyAlly = CreateCombatant("party-2-ally", CombatTeam.Friendly, [], partyNumber: 2);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [enemyAreaDamage]);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([caster, samePartyAlly, otherPartyAlly], [enemy]);

        Assert.Equal(
            ["caster", "party-1-ally"],
            result.EventLog
                .Where(log => log.Source == "effect.party.barrier" && log.EventType == EventType.RestoreBarrier)
                .Select(log => log.TargetId)
                .ToArray());
        Assert.Equal(
            ["caster", "party-1-ally", "party-2-ally"],
            result.EventLog
                .Where(log => log.Source == "effect.enemy.area" && log.EventType == EventType.Damage)
                .Select(log => log.TargetId)
                .ToArray());
    }

    [Fact]
    public void Current_target_is_locked_for_every_effect_in_an_active_ability_use()
    {
        var ability = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.locked.target",
            Kind = AbilitySpecKind.Active,
            Name = "Locked Target",
            Effects =
            [
                new()
                {
                    Id = "effect.locked.target.one",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 5
                },
                new()
                {
                    Id = "effect.locked.target.two",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 5
                }
            ]
        });
        var caster = CreateCombatant("caster", CombatTeam.Friendly, [ability]);
        var firstEnemy = CreateCombatant("enemy-1", CombatTeam.Hostile, [], maxHealth: 1_000);
        var secondEnemy = CreateCombatant("enemy-2", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 27));

        var result = engine.Run([caster], [firstEnemy, secondEnemy]);
        var targets = result.EventLog
            .Where(log => log.Source.StartsWith("effect.locked.target", StringComparison.Ordinal)
                && log.EventType == EventType.Damage)
            .Select(log => log.TargetId)
            .ToArray();

        Assert.Equal(2, targets.Length);
        Assert.Single(targets.Distinct());
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
    public void Forest_spirit_bloom_counts_only_heals_applied_by_its_owner()
    {
        var spiritBloom = CreateSpiritBloomPassive();
        var alliedHeals = new AbilitySpec
        {
            Id = "ability.ally.three.heals",
            Kind = AbilitySpecKind.Active,
            Name = "Ally Three Heals",
            CooldownTicks = 1000,
            Costs = [new() { Resource = AbilityResourceType.Health, BaseValue = 100 }],
            Effects =
            [
                CreateFixedHeal("effect.ally.heal.one"),
                CreateFixedHeal("effect.ally.heal.two"),
                CreateFixedHeal("effect.ally.heal.three")
            ]
        };
        var owner = CreateCombatant(
            "forest-spirit-owner",
            CombatTeam.Friendly,
            [AbilityCompiler.CompileAbility(spiritBloom)]);
        var ally = CreateCombatant(
            "ally-healer",
            CombatTeam.Friendly,
            [AbilityCompiler.CompileAbility(alliedHeals)]);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([owner, ally], [hostile]);

        Assert.DoesNotContain(
            result.EventLog,
            entry => entry.Source == "effect.forest.spirit.bloom" && entry.EventType == EventType.Heal);
    }

    [Fact]
    public void Forest_spirit_bloom_counts_each_owner_hot_application_once_not_each_tick()
    {
        var spiritBloom = CreateSpiritBloomPassive();
        var threeHealOverTimeApplications = new AbilitySpec
        {
            Id = "ability.owner.three.hots",
            Kind = AbilitySpecKind.Active,
            Name = "Three Heal Over Time Applications",
            CooldownTicks = 1000,
            Costs = [new() { Resource = AbilityResourceType.Health, BaseValue = 100 }],
            Effects =
            [
                CreateFixedHeal("effect.owner.hot.one", durationTicks: 40, intervalTicks: 10),
                CreateFixedHeal("effect.owner.hot.two", durationTicks: 40, intervalTicks: 10),
                CreateFixedHeal("effect.owner.hot.three", durationTicks: 40, intervalTicks: 10)
            ]
        };
        var owner = CreateCombatant(
            "forest-spirit-owner",
            CombatTeam.Friendly,
            AbilityCompiler.CompileAbilities([spiritBloom, threeHealOverTimeApplications]).Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 41, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([owner], [hostile]);

        Assert.Single(
            result.EventLog,
            entry => entry.Source == "effect.forest.spirit.bloom" && entry.EventType == EventType.Heal);
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
        Assert.Single(
            new[] { firstHostile, secondHostile },
            hostile => hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
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
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 30, RandomSeed: 1));

        var result = engine.Run([friendly], [hostile]);

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
    public void Json_catalog_reuses_compiled_immutable_definitions()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());

        var first = provider.GetCompiledCatalog();
        var second = provider.GetCompiledCatalog();

        Assert.Same(first, second);
        Assert.Same(
            first.AbilitiesById["ability.creature.large_rat.big"],
            second.AbilitiesById["ability.creature.large_rat.big"]);
        Assert.Equal(provider.GetCatalog().Abilities.Count, first.AbilitiesById.Count);
        Assert.Equal(provider.GetCatalog().Statuses.Count, first.StatusesById.Count);
        Assert.Equal(provider.GetCatalog().Summons.Count, first.SummonsById.Count);
    }

    [Fact]
    public void Json_catalog_garran_authors_the_requested_floor_one_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        var abilityIds = profile.GetAbilityIds("monster.garran,_the_gatekeeper");

        Assert.Equal(4, abilityIds.Count);
        Assert.Contains("ability.creature.garran.gatehammer", abilityIds);
        Assert.Contains("ability.creature.garran.slam_the_gates", abilityIds);
        Assert.Contains("ability.creature.garran.gatekeepers_toll", abilityIds);
        Assert.Contains("ability.creature.garran.the_first_gate", abilityIds);

        var gatehammer = catalog.AbilitiesById["ability.creature.garran.gatehammer"];
        var gatehammerDamage = Assert.Single(gatehammer.Effects);
        Assert.Equal(80, gatehammer.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.CurrentTarget, gatehammerDamage.Target);
        Assert.Equal(DamageType.Physical, gatehammerDamage.DamageType);
        Assert.Equal(2.3f, gatehammerDamage.ScalingCoefficient);

        var slamTheGates = catalog.AbilitiesById["ability.creature.garran.slam_the_gates"];
        var slamDamage = Assert.Single(slamTheGates.Effects);
        Assert.Equal(140, slamTheGates.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.AllEnemies, slamDamage.Target);
        Assert.Equal(DamageType.Magical, slamDamage.DamageType);
        Assert.Equal(1.5f, slamDamage.ScalingCoefficient);

        var gatekeepersToll = catalog.AbilitiesById["ability.creature.garran.gatekeepers_toll"];
        var tollTransfer = Assert.Single(gatekeepersToll.Effects);
        Assert.Equal(180, gatekeepersToll.CooldownTicks);
        Assert.Equal(AbilityEffectOperation.TransferAttributePercent, tollTransfer.Operation);
        Assert.Equal(AbilityTargetSelector.RandomEnemy, tollTransfer.Target);
        Assert.Equal(AttributeType.Power, tollTransfer.Attribute);
        Assert.Equal(0.15f, tollTransfer.ScalingCoefficient);
        Assert.Equal(3, catalog.StatusesById["status.garran.gate_seal"].MaxStacks);
    }

    [Fact]
    public void Json_catalog_morrowmaw_authors_the_requested_floor_three_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        var abilityIds = profile.GetAbilityIds("monster.morrowmaw,_broodkeeper");

        Assert.Equal(4, abilityIds.Count);
        Assert.Contains("ability.creature.morrowmaw.hatch_the_brood", abilityIds);
        Assert.Contains("ability.creature.morrowmaw.spore_eruption", abilityIds);
        Assert.Contains("ability.creature.morrowmaw.devour_the_weak", abilityIds);
        Assert.Contains("ability.creature.morrowmaw.broodmother", abilityIds);

        var hatch = catalog.AbilitiesById["ability.creature.morrowmaw.hatch_the_brood"];
        var hatchEffect = Assert.Single(hatch.Effects);
        Assert.Equal(180, hatch.CooldownTicks);
        Assert.Equal(AbilityEffectOperation.Summon, hatchEffect.Operation);
        Assert.Equal(AbilityTargetSelector.AllEnemies, hatchEffect.Target);
        Assert.Equal("morrowmawBroodling", hatchEffect.SummonId);

        var eruption = catalog.AbilitiesById["ability.creature.morrowmaw.spore_eruption"];
        var eruptionDamage = Assert.Single(eruption.Effects);
        Assert.Equal(120, eruption.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.AllEnemies, eruptionDamage.Target);
        Assert.Equal(DamageType.Magical, eruptionDamage.DamageType);
        Assert.Equal(0.55f, eruptionDamage.ScalingCoefficient);

        var devour = catalog.AbilitiesById["ability.creature.morrowmaw.devour_the_weak"];
        var devourEffect = Assert.Single(devour.Effects);
        Assert.Equal(200, devour.CooldownTicks);
        Assert.Equal(AbilityEffectOperation.ConsumeOwnedSummon, devourEffect.Operation);
        Assert.Equal(AbilityTargetSelector.Self, devourEffect.Target);
        Assert.Equal(AttributeType.MaxHealth, devourEffect.ScalingAttribute);
        Assert.Equal(0.04f, devourEffect.ScalingCoefficient);

        var broodling = catalog.SummonsById["morrowmawBroodling"];
        Assert.Contains("ability.summon.morrowmaw_broodling.venomous_bite", broodling.AbilityIds);
        var broodlingHealth = broodling.Attributes.Single(attribute =>
            attribute.Attribute == AttributeType.MaxHealth);
        Assert.Equal(AttributeType.MaxHealth, broodlingHealth.ScalingAttribute);
        Assert.Equal(0.1f, broodlingHealth.ScalingCoefficient);

        var venomousBite = catalog.AbilitiesById["ability.summon.morrowmaw_broodling.venomous_bite"];
        var poison = Assert.Single(venomousBite.Effects);
        Assert.Equal(100, venomousBite.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.RandomEnemy, poison.Target);
        Assert.Equal(AbilityEffectOperation.ApplyCondition, poison.Operation);
        Assert.Equal(StandardConditionType.Poison, poison.Condition);
        Assert.Equal(10, poison.BaseValue);
    }

    [Fact]
    public void Morrowmaws_brood_count_drives_broodmother_and_devour_the_weak()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilityIds = new[]
        {
            "ability.creature.morrowmaw.hatch_the_brood",
            "ability.creature.morrowmaw.devour_the_weak",
            "ability.creature.morrowmaw.broodmother"
        };
        var abilities = AbilityCompiler.CompileAbilities(
            abilityIds.Select(id => catalog.AbilitiesById[id]));
        var morrowmaw = CreateCombatant("morrowmaw", CombatTeam.Friendly, abilityIds.Select(id => abilities[id]));
        morrowmaw.SetHealth(100);
        var enemies = new[]
        {
            CreateCombatant("enemy-1", CombatTeam.Hostile, []),
            CreateCombatant("enemy-2", CombatTeam.Hostile, []),
            CreateCombatant("enemy-3", CombatTeam.Hostile, [])
        };
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 2, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([morrowmaw], enemies);

        Assert.Equal(
            3,
            result.EventLog.Count(log =>
                log.Source == "effect.creature.morrowmaw.hatch_the_brood.summon"
                && log.EventType == EventType.Summon));
        Assert.All(
            result.EventLog.Where(log =>
                log.Source == "effect.creature.morrowmaw.hatch_the_brood.summon"
                && log.EventType == EventType.Summon),
            log => Assert.Equal(20, log.CombatEntity?.MaxHealth));
        Assert.Equal(
            2,
            result.EventLog.Count(log =>
                log.Source == "condition.poison"
                && log.EventType == EventType.StatusEffect
                && log.ActorId.Contains(":summon:morrowmawBroodling:", StringComparison.Ordinal)));
        Assert.Single(result.EventLog, log =>
            log.Source == "effect.creature.morrowmaw.devour_the_weak.consume"
            && log.EventType == EventType.SummonExpired);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.morrowmaw.devour_the_weak.consume"
            && log.EventType == EventType.Heal
            && log.Magnitude == 8);
        Assert.Equal(12, morrowmaw.GetAttribute(AttributeType.DamageReduction));
        Assert.Equal(10, morrowmaw.GetAttribute(AttributeType.AttackSpeed));
    }

    [Fact]
    public void Devour_the_weak_consumes_the_lowest_health_matching_broodling()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var devour = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.morrowmaw.devour_the_weak"]);
        var morrowmaw = CreateCombatant("morrowmaw", CombatTeam.Friendly, [devour]);
        morrowmaw.SetHealth(100);
        var strongerBroodling = CreateOwnedBroodling("stronger-broodling", morrowmaw, health: 30);
        var weakerBroodling = CreateOwnedBroodling("weaker-broodling", morrowmaw, health: 10);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([morrowmaw, strongerBroodling, weakerBroodling], [enemy]);

        Assert.Equal(0, weakerBroodling.Health);
        Assert.Equal(30, strongerBroodling.Health);
        Assert.Equal(108, morrowmaw.Health);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.morrowmaw.devour_the_weak.consume"
            && log.EventType == EventType.SummonExpired
            && log.TargetId == "weaker-broodling");
    }

    [Fact]
    public void Json_catalog_velka_authors_the_requested_floor_two_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        var abilityIds = profile.GetAbilityIds("monster.velka,_the_bloodwing_huntress");

        Assert.Equal(4, abilityIds.Count);
        Assert.Contains("ability.creature.velka.rending_dive", abilityIds);
        Assert.Contains("ability.creature.velka.crimson_gale", abilityIds);
        Assert.Contains("ability.creature.velka.feast_on_wounds", abilityIds);
        Assert.Contains("ability.creature.velka.scent_of_weakness", abilityIds);
        Assert.Equal(100, catalog.AbilitiesById["ability.creature.velka.rending_dive"].CooldownTicks);
        Assert.Equal(130, catalog.AbilitiesById["ability.creature.velka.crimson_gale"].CooldownTicks);
        Assert.Equal(210, catalog.AbilitiesById["ability.creature.velka.feast_on_wounds"].CooldownTicks);
    }

    [Fact]
    public void Velkas_rending_dive_damages_and_bleeds_the_same_lowest_health_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var rendingDive = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.velka.rending_dive"]);
        var velka = CreateCombatant("velka", CombatTeam.Hostile, [rendingDive]);
        var lowestHealthEnemy = CreateCombatant("lowest", CombatTeam.Friendly, [], maxHealth: 1_000);
        var otherEnemy = CreateCombatant("other", CombatTeam.Friendly, [], maxHealth: 1_000);
        lowestHealthEnemy.AdjustHealth(-900);
        otherEnemy.AdjustHealth(-800);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([lowestHealthEnemy, otherEnemy], [velka]);

        Assert.True(lowestHealthEnemy.Health < 100);
        Assert.Equal(12, lowestHealthEnemy.GetConditionStacks(StandardConditionType.Bleed));
        Assert.Equal(0, otherEnemy.GetConditionStacks(StandardConditionType.Bleed));
    }

    [Fact]
    public void Velkas_feast_consumes_three_bleed_per_enemy_and_caps_total_healing()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var feast = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.velka.feast_on_wounds"]);
        var velka = CreateCombatant("velka", CombatTeam.Hostile, [feast], maxHealth: 1_000);
        velka.AdjustHealth(-500);
        var enemies = Enumerable.Range(1, 3)
            .Select(index => CreateCombatant($"enemy-{index}", CombatTeam.Friendly, [], maxHealth: 1_000))
            .ToArray();
        for (var index = 0; index < enemies.Length; index++)
        {
            enemies[index].Conditions.Add(new RuntimeCondition(
                StandardConditionType.Bleed,
                velka,
                enemies[index],
                value: 4,
                durationTicks: 100,
                powerSnapshot: 50,
                applicationOrder: index + 1,
                statsSource: "Test Bleed"));
        }
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run(enemies, [velka]);

        Assert.Equal(540, velka.Health);
        Assert.All(enemies, enemy => Assert.Equal(1, enemy.GetConditionStacks(StandardConditionType.Bleed)));
    }

    [Fact]
    public void Velkas_scent_tracks_whether_any_living_enemy_is_below_thirty_percent_health()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var scent = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.velka.scent_of_weakness"]);
        var selfDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.velka.self_damage",
            Kind = AbilitySpecKind.Active,
            Name = "Self Damage",
            Effects =
            [
                new()
                {
                    Id = "effect.test.velka.self_damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 150
                }
            ]
        });
        var selfHeal = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.velka.self_heal",
            Kind = AbilitySpecKind.Active,
            Name = "Self Heal",
            Effects =
            [
                new()
                {
                    Id = "effect.test.velka.self_heal",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 150
                }
            ]
        });
        var enemy = CreateCombatant("enemy", CombatTeam.Friendly, [selfDamage, selfHeal]);
        var velka = CreateCombatant("velka", CombatTeam.Hostile, [scent]);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([enemy], [velka]);

        Assert.False(velka.HasCondition(StandardConditionType.Haste));
        Assert.Contains(result.EventLog, log =>
            log.EventType == EventType.StatusEffect
            && log.Details.Contains("applied Haste", StringComparison.Ordinal));
        Assert.Contains(result.EventLog, log =>
            log.EventType == EventType.StatusEffectRemoved
            && log.Details.Contains("Haste was removed", StringComparison.Ordinal));
    }

    [Fact]
    public void Json_catalog_vaelor_authors_the_requested_floor_four_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        var abilityIds = profile.GetAbilityIds("monster.vaelor,_the_mirrorbound");

        Assert.Equal(4, abilityIds.Count);
        Assert.Contains("ability.creature.vaelor.mirror_lance", abilityIds);
        Assert.Contains("ability.creature.vaelor.hall_of_shards", abilityIds);
        Assert.Contains("ability.creature.vaelor.reflective_mirrorplate", abilityIds);
        Assert.Contains("ability.creature.vaelor.mirrorbound", abilityIds);
        Assert.Equal(140, catalog.AbilitiesById["ability.creature.vaelor.mirror_lance"].CooldownTicks);
        Assert.Equal(160, catalog.AbilitiesById["ability.creature.vaelor.hall_of_shards"].CooldownTicks);
        Assert.Equal(220, catalog.AbilitiesById["ability.creature.vaelor.reflective_mirrorplate"].CooldownTicks);
        Assert.Equal(30, catalog.StatusesById["status.vaelor.next_magical_damage"].MaxStacks);
        Assert.Equal(30, catalog.StatusesById["status.vaelor.next_physical_damage"].MaxStacks);

        var mirrorLance = catalog.AbilitiesById["ability.creature.vaelor.mirror_lance"];
        Assert.Equal(AbilityTargetSelector.HighestHealthEnemy, mirrorLance.Effects[0].Target);
        Assert.Equal(AbilityTargetSelector.LowestCurrentHealthEnemy, mirrorLance.Effects[1].Target);
        var mirrorplate = catalog.AbilitiesById["ability.creature.vaelor.reflective_mirrorplate"];
        var reflection = Assert.Single(mirrorplate.Effects);
        Assert.Equal(StandardConditionType.Thorns, reflection.Condition);
        Assert.Equal(100, reflection.BaseValue);
        Assert.Equal(50, reflection.DurationTicks);
    }

    [Fact]
    public void Json_catalog_kharad_authors_the_requested_floor_five_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        var abilityIds = profile.GetAbilityIds("monster.kharad,_the_first_warden");

        Assert.Equal(4, abilityIds.Count);
        Assert.Contains("ability.creature.kharad.crushing_verdict", abilityIds);
        Assert.Contains("ability.creature.kharad.raise_the_twin_pillars", abilityIds);
        Assert.Contains("ability.creature.kharad.seal_of_ascension", abilityIds);
        Assert.Contains("ability.creature.kharad.keystone_resonance", abilityIds);
        Assert.Equal(140, catalog.AbilitiesById["ability.creature.kharad.crushing_verdict"].CooldownTicks);
        Assert.Equal(220, catalog.AbilitiesById["ability.creature.kharad.raise_the_twin_pillars"].CooldownTicks);
        Assert.Equal(160, catalog.AbilitiesById["ability.creature.kharad.seal_of_ascension"].CooldownTicks);

        var crushingDamage = catalog.AbilitiesById["ability.creature.kharad.crushing_verdict"].Effects[0];
        Assert.Equal(AbilityTargetSelector.HighestMaxHealthEnemy, crushingDamage.Target);
        Assert.Equal(2.6f, crushingDamage.ScalingCoefficient);

        Assert.False(catalog.SummonsById["kharadIronPillar"].CanBasicAttack);
        Assert.False(catalog.SummonsById["kharadAetherPillar"].CanBasicAttack);
        Assert.All(
            new[] { "kharadIronPillar", "kharadAetherPillar" },
            summonId => Assert.Equal(
                0.1f,
                catalog.SummonsById[summonId].Attributes.Single(attribute =>
                    attribute.Attribute == AttributeType.MaxHealth).ScalingCoefficient));

        var resonance = catalog.StatusesById["status.kharad.resonance"];
        Assert.Equal(5, resonance.MaxStacks);
        Assert.True(resonance.LockAtMaxStacks);
    }

    [Fact]
    public void Json_catalog_orsenn_authors_the_requested_floor_six_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        Assert.Equal(
            [
                "ability.creature.orsenn.ashen_toll",
                "ability.creature.orsenn.funeral_brand",
                "ability.creature.orsenn.cremation",
                "ability.creature.orsenn.cinderbound"
            ],
            profile.GetAbilityIds("monster.orsenn,_the_ashen_bellkeeper"));

        var ashenToll = catalog.AbilitiesById["ability.creature.orsenn.ashen_toll"];
        Assert.Equal(60, ashenToll.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.AllEnemies, ashenToll.Effects[0].Target);
        Assert.Equal(0.3f, ashenToll.Effects[0].ScalingCoefficient);
        Assert.Equal("status.orsenn.cinder", ashenToll.Effects[1].StatusId);

        var funeralBrand = catalog.AbilitiesById["ability.creature.orsenn.funeral_brand"];
        Assert.Equal(150, funeralBrand.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.HighestHealthEnemy, funeralBrand.Effects[0].Target);
        Assert.Equal(2, funeralBrand.Effects[0].BaseValue);
        Assert.Equal(StandardConditionType.Doom, funeralBrand.Effects[1].Condition);
        Assert.Equal(500, funeralBrand.Effects[1].BaseValue);

        var cremation = catalog.AbilitiesById["ability.creature.orsenn.cremation"];
        Assert.Equal(450, cremation.CooldownTicks);
        Assert.Equal(0.45f, cremation.Effects[0].ScalingCoefficient);
        Assert.Equal("status.orsenn.cinder", cremation.Effects[0].ScalingStatusId);
        Assert.Equal(AbilityConditionSubject.Target, cremation.Effects[0].ScalingStatusSubject);
        Assert.Equal(0.1125f, cremation.Effects[0].StatusScalingCoefficient);
        Assert.Equal(AbilityEffectOperation.RemoveStatus, cremation.Effects[1].Operation);

        var cinder = catalog.StatusesById["status.orsenn.cinder"];
        Assert.Equal(6, cinder.MaxStacks);
        Assert.Equal(6, cinder.SourceDamageTakenPercentPerStack);
        var combustion = cinder.Effects[0];
        Assert.Equal(AttributeType.MaxHealth, combustion.ScalingAttribute);
        Assert.Equal(AbilityConditionSubject.Target, combustion.ScalingAttributeSubject);
        Assert.Equal(0.15f, combustion.ScalingCoefficient);
    }

    [Fact]
    public void Orsenns_funeral_brand_marks_and_dooms_the_same_highest_health_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var funeralBrand = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.orsenn.funeral_brand"]);
        var orsenn = CreateCombatant("orsenn", CombatTeam.Friendly, [funeralBrand]);
        var lowerHealth = CreateCombatant("lower-health", CombatTeam.Hostile, [], maxHealth: 500);
        var higherHealth = CreateCombatant("higher-health", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([orsenn], [lowerHealth, higherHealth]);

        Assert.Equal(0, lowerHealth.GetStatusStacks("status.orsenn.cinder"));
        Assert.Equal(0, lowerHealth.GetConditionStacks(StandardConditionType.Doom));
        Assert.Equal(2, higherHealth.GetStatusStacks("status.orsenn.cinder"));
        Assert.Equal(500, higherHealth.GetConditionStacks(StandardConditionType.Doom));
    }

    [Fact]
    public void Cinder_amplifies_only_its_sources_damage()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var orsennAbility = AbilityCompiler.CompileAbility(
            CreateFixedDamageAbility("ability.test.orsenn", "effect.test.orsenn", 100));
        var allyAbility = AbilityCompiler.CompileAbility(
            CreateFixedDamageAbility("ability.test.ally", "effect.test.ally", 100));
        var orsenn = CreateCombatant("orsenn", CombatTeam.Friendly, [orsennAbility]);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, [allyAbility]);
        var target = CreateCombatant("target", CombatTeam.Hostile, [], maxHealth: 1_000);
        target.Statuses.Add(new RuntimeStatus(statuses["status.orsenn.cinder"], orsenn, target, 3));
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([orsenn, ally], [target]);

        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.test.orsenn" && log.EventType == EventType.Damage && log.Magnitude == 118);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.test.ally" && log.EventType == EventType.Damage && log.Magnitude == 100);
    }

    [Fact]
    public void Six_cinder_combusts_from_target_max_health_and_removes_all_stacks()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var applyCinder = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.apply-cinder",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Cinder",
            Effects =
            [
                new()
                {
                    Id = "effect.test.apply-cinder",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 1,
                    StatusId = "status.orsenn.cinder"
                }
            ]
        });
        var orsenn = CreateCombatant("orsenn", CombatTeam.Friendly, [applyCinder]);
        var target = CreateCombatant("target", CombatTeam.Hostile, [], maxHealth: 1_000);
        target.Statuses.Add(new RuntimeStatus(statuses["status.orsenn.cinder"], orsenn, target, 5));
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([orsenn], [target]);

        Assert.Equal(0, target.GetStatusStacks("status.orsenn.cinder"));
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.status.orsenn.cinder.combust"
            && log.EventType == EventType.Damage
            && log.Magnitude == 204);
    }

    [Fact]
    public void Cremation_scales_per_targets_cinder_then_removes_it_from_every_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var cremation = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.orsenn.cremation"]);
        var orsenn = CreateCombatant("orsenn", CombatTeam.Friendly, [cremation]);
        var oneCinder = CreateCombatant("one-cinder", CombatTeam.Hostile, [], maxHealth: 1_000);
        var threeCinder = CreateCombatant("three-cinder", CombatTeam.Hostile, [], maxHealth: 1_000);
        oneCinder.Statuses.Add(new RuntimeStatus(statuses["status.orsenn.cinder"], orsenn, oneCinder, 1));
        threeCinder.Statuses.Add(new RuntimeStatus(statuses["status.orsenn.cinder"], orsenn, threeCinder, 3));
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([orsenn], [oneCinder, threeCinder]);

        var oneCinderDamage = Assert.Single(result.EventLog, log =>
            log.Source == "effect.creature.orsenn.cremation.damage" && log.TargetId == oneCinder.Id);
        var threeCinderDamage = Assert.Single(result.EventLog, log =>
            log.Source == "effect.creature.orsenn.cremation.damage" && log.TargetId == threeCinder.Id);
        Assert.True(threeCinderDamage.Magnitude > oneCinderDamage.Magnitude);
        Assert.Equal(0, oneCinder.GetStatusStacks("status.orsenn.cinder"));
        Assert.Equal(0, threeCinder.GetStatusStacks("status.orsenn.cinder"));
    }

    [Fact]
    public void Json_catalog_ni_authors_the_requested_floor_nine_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        Assert.Equal(
            [
                "ability.creature.ni.ninefold_strike",
                "ability.creature.ni.ninth_seal",
                "ability.creature.ni.one_among_nine",
                "ability.creature.ni.ninefold"
            ],
            profile.GetAbilityIds("monster.ni,_the_ninefold"));

        var strike = catalog.AbilitiesById["ability.creature.ni.ninefold_strike"];
        Assert.Equal(80, strike.CooldownTicks);
        Assert.Equal(1f, strike.Effects[0].ScalingCoefficient);
        Assert.Equal(0.2f, strike.Effects[1].ScalingCoefficient);
        Assert.Equal("niCopy", strike.Effects[1].RepeatPerOwnedSummonId);

        var seal = catalog.AbilitiesById["ability.creature.ni.ninth_seal"];
        Assert.Equal(160, seal.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.AllEnemies, seal.Effects[0].Target);
        Assert.Equal("niCopy", seal.Effects[0].ScalingOwnedSummonId);
        Assert.Equal(0.2f, seal.Effects[0].OwnedSummonScalingCoefficient);

        var swap = Assert.Single(catalog.AbilitiesById["ability.creature.ni.one_among_nine"].Effects);
        Assert.Equal(AbilityEffectOperation.SwapHealth, swap.Operation);
        Assert.Equal(AbilityTargetSelector.HighestCurrentHealthOwnedSummon, swap.Target);
        Assert.Equal("niCopy", swap.SummonId);

        var passive = catalog.AbilitiesById["ability.creature.ni.ninefold"];
        Assert.Equal(9, passive.Effects[0].RepeatCount);
        Assert.Equal(0.05f, passive.Effects[1].ScalingCoefficient);
        var copy = catalog.SummonsById["niCopy"];
        Assert.Equal(9, copy.MaxActive);
        Assert.False(copy.CanBasicAttack);
        Assert.Equal(
            0.1f,
            copy.Attributes.Single(attribute => attribute.Attribute == AttributeType.MaxHealth).ScalingCoefficient);
    }

    [Fact]
    public void Ninefold_summons_nine_inert_copies_and_strike_repeats_for_each_survivor()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var selectedIds = new[]
        {
            "ability.creature.ni.ninefold_strike",
            "ability.creature.ni.ninefold"
        };
        var abilities = AbilityCompiler.CompileAbilities(selectedIds.Select(id => catalog.AbilitiesById[id]));
        var ni = CreateCombatant("ni", CombatTeam.Friendly, selectedIds.Select(id => abilities[id]), maxHealth: 1_000);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([ni], [enemy]);

        Assert.Equal(9, result.EventLog.Count(log =>
            log.Source == "effect.creature.ni.ninefold.summon" && log.EventType == EventType.Summon));
        Assert.Equal(9, result.EventLog.Count(log =>
            log.Source == "effect.creature.ni.ninefold_strike.copy" && log.EventType == EventType.Damage));
        Assert.DoesNotContain(result.EventLog, log =>
            log.ActorId.Contains(":summon:niCopy:", StringComparison.Ordinal)
            && log.EventType == EventType.AbilityUse);
    }

    [Fact]
    public void Ninth_seal_scales_with_the_number_of_living_copies()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var seal = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninth_seal"]);

        int Run(int copyCount)
        {
            var ni = CreateCombatant("ni", CombatTeam.Friendly, [seal]);
            var copies = Enumerable.Range(1, copyCount)
                .Select(index => CreateOwnedNiCopy($"copy-{index}", ni, 20))
                .ToArray();
            var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10_000);
            var engine = new FastCombatEngine(
                new Dictionary<string, CompiledStatus>(),
                new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));
            var result = engine.Run([ni, .. copies], [enemy]);
            return Assert.Single(result.EventLog, log =>
                log.Source == "effect.creature.ni.ninth_seal.damage" && log.EventType == EventType.Damage).Magnitude;
        }

        Assert.True(Run(3) > Run(1));
    }

    [Fact]
    public void One_among_nine_swaps_with_the_healthiest_copy_only_when_it_is_healthier()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var swap = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.one_among_nine"]);
        var ni = CreateCombatant("ni", CombatTeam.Friendly, [swap], maxHealth: 100);
        ni.SetHealth(20);
        var lowerCopy = CreateOwnedNiCopy("lower-copy", ni, 60);
        var higherCopy = CreateOwnedNiCopy("higher-copy", ni, 80);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([ni, lowerCopy, higherCopy], [enemy]);

        Assert.Equal(80, ni.Health);
        Assert.Equal(20, higherCopy.Health);
        Assert.Equal(60, lowerCopy.Health);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.ni.one_among_nine.swap" && log.TargetId == higherCopy.Id);
    }

    [Fact]
    public void Ninefold_permanently_grants_power_for_each_copy_that_dies()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var passive = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninefold"]);
        var sweep = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.copy-sweep",
            Kind = AbilitySpecKind.Active,
            Name = "Copy Sweep",
            Effects =
            [
                new()
                {
                    Id = "effect.test.copy-sweep",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.AllEnemies,
                    BaseValue = 110,
                    DamageType = DamageType.None,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        });
        var ni = CreateCombatant("ni", CombatTeam.Friendly, [passive], maxHealth: 1_000);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [sweep], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([ni], [enemy]);

        Assert.Equal(68, ni.GetAttribute(AttributeType.Power));
        Assert.Equal(9, result.EventLog.Count(log =>
            log.Source == "effect.creature.ni.ninefold.power" && log.EventType == EventType.Buff));
    }

    [Fact]
    public void Json_catalog_kodoku_authors_the_requested_floor_eight_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        Assert.Equal(
            [
                "ability.creature.kodoku.insect_jar",
                "ability.creature.kodoku.thousand_poisons",
                "ability.creature.kodoku.withering_miasma",
                "ability.creature.kodoku.survivors_struggle"
            ],
            profile.GetAbilityIds("monster.kodoku,_the_poisoned_vessel"));

        var insectJar = Assert.Single(catalog.AbilitiesById["ability.creature.kodoku.insect_jar"].Effects);
        Assert.Equal(150, catalog.AbilitiesById["ability.creature.kodoku.insect_jar"].CooldownTicks);
        Assert.Equal(3, insectJar.RepeatCount);
        Assert.Equal("venomSpawn", insectJar.SummonId);
        Assert.Equal(AttributeType.MaxHealth, insectJar.HealingScalingAttribute);
        Assert.Equal(0.03f, insectJar.HealingScalingCoefficient);

        var poison = Assert.Single(catalog.AbilitiesById["ability.creature.kodoku.thousand_poisons"].Effects);
        Assert.Equal(AbilityTargetSelector.LowestCurrentHealthEnemy, poison.Target);
        Assert.Equal(StandardConditionType.Poison, poison.Condition);
        Assert.Equal(200, poison.BaseValue);

        var miasma = catalog.AbilitiesById["ability.creature.kodoku.withering_miasma"];
        Assert.Equal(150, miasma.CooldownTicks);
        Assert.All(miasma.Effects, effect =>
        {
            Assert.Equal(AbilityTargetSelector.AllEnemies, effect.Target);
            Assert.Equal(-80, effect.BaseValue);
            Assert.Equal(150, effect.DurationTicks);
        });

        var struggle = catalog.AbilitiesById["ability.creature.kodoku.survivors_struggle"];
        Assert.Equal(5, struggle.Effects[0].RepeatCount);
        Assert.All(struggle.Effects.Skip(1), effect =>
        {
            Assert.Equal(AbilityTargetSelector.OwnedSummons, effect.Target);
            Assert.Equal("venomSpawn", effect.SummonId);
        });

        var venomspawn = catalog.SummonsById["venomSpawn"];
        Assert.Equal(5, venomspawn.MaxActive);
        Assert.True(venomspawn.CanBasicAttack);
        Assert.Equal(
            0.08f,
            venomspawn.Attributes.Single(attribute => attribute.Attribute == AttributeType.MaxHealth).ScalingCoefficient);
        Assert.Equal(
            0.15f,
            venomspawn.Attributes.Single(attribute => attribute.Attribute == AttributeType.Power).ScalingCoefficient);
    }

    [Theory]
    [InlineData(2, 3, 0)]
    [InlineData(3, 2, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(5, 0, 3)]
    public void Insect_jar_summons_to_five_then_heals_once_per_excess(
        int existingCount,
        int expectedSummons,
        int expectedOverflowHeals)
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var insectJar = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.kodoku.insect_jar"]);
        var kodoku = CreateCombatant("kodoku", CombatTeam.Friendly, [insectJar], maxHealth: 1_000);
        kodoku.SetHealth(500);
        var existing = Enumerable.Range(1, existingCount)
            .Select(index => CreateOwnedVenomspawn($"venomspawn-{index}", kodoku, 80))
            .ToArray();
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([kodoku, .. existing], [enemy]);

        Assert.Equal(expectedSummons, result.EventLog.Count(log =>
            log.Source == "effect.creature.kodoku.insect_jar.summon" && log.EventType == EventType.Summon));
        Assert.Equal(expectedOverflowHeals, result.EventLog.Count(log =>
            log.Source == "effect.creature.kodoku.insect_jar.summon" && log.EventType == EventType.Heal));
        Assert.Equal(500 + expectedOverflowHeals * 30, kodoku.Health);
    }

    [Fact]
    public void Thousand_poisons_targets_the_enemy_with_lowest_current_health()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var poison = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.kodoku.thousand_poisons"]);
        var kodoku = CreateCombatant("kodoku", CombatTeam.Friendly, [poison]);
        var lower = CreateCombatant("lower", CombatTeam.Hostile, [], maxHealth: 1_000);
        lower.SetHealth(100);
        var higher = CreateCombatant("higher", CombatTeam.Hostile, [], maxHealth: 200);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([kodoku], [higher, lower]);

        Assert.Equal(200, lower.GetConditionStacks(StandardConditionType.Poison));
        Assert.Equal(0, higher.GetConditionStacks(StandardConditionType.Poison));
    }

    [Fact]
    public void Withering_miasma_reduces_healing_and_regeneration_on_every_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var miasma = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.kodoku.withering_miasma"]);
        var kodoku = CreateCombatant("kodoku", CombatTeam.Friendly, [miasma]);
        var first = CreateCombatant("first", CombatTeam.Hostile, []);
        var second = CreateCombatant("second", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([kodoku], [first, second]);

        Assert.All(new[] { first, second }, target =>
        {
            Assert.Equal(-80, target.HealingReceivedPercent);
            Assert.Equal(-80, target.RegenerationRatePercent);
        });
    }

    [Fact]
    public void Venomspawn_deaths_permanently_stack_power_and_attack_speed_on_survivors()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var struggle = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.kodoku.survivors_struggle"]);
        var sweep = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.venomspawn-sweep",
            Kind = AbilitySpecKind.Active,
            Name = "Venomspawn Sweep",
            Effects =
            [
                new()
                {
                    Id = "effect.test.venomspawn-sweep",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.AllEnemies,
                    BaseValue = 20,
                    DamageType = DamageType.None,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        });
        var kodoku = CreateCombatant("kodoku", CombatTeam.Friendly, [struggle], maxHealth: 1_000);
        var firstDoomed = CreateOwnedVenomspawn("first-doomed", kodoku, 10);
        var secondDoomed = CreateOwnedVenomspawn("second-doomed", kodoku, 10);
        var survivors = Enumerable.Range(1, 3)
            .Select(index => CreateOwnedVenomspawn($"survivor-{index}", kodoku, 80))
            .ToArray();
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [sweep], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([kodoku, firstDoomed, secondDoomed, .. survivors], [enemy]);

        Assert.False(firstDoomed.IsAlive);
        Assert.False(secondDoomed.IsAlive);
        Assert.All(survivors, survivor =>
        {
            Assert.Equal(30, survivor.GetAttribute(AttributeType.Power));
            Assert.Equal(50, survivor.GetAttribute(AttributeType.AttackSpeed));
        });
        Assert.Equal(50, kodoku.GetAttribute(AttributeType.Power));
    }

    [Fact]
    public void Json_catalog_eydis_authors_the_requested_floor_seven_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        Assert.Equal(
            [
                "ability.creature.eydis.springtide",
                "ability.creature.eydis.ancient_heartwood",
                "ability.creature.eydis.tranquil_waters",
                "ability.creature.eydis.endless_spring"
            ],
            profile.GetAbilityIds("monster.eydis,_the_endless_spring"));

        var springtide = Assert.Single(catalog.AbilitiesById["ability.creature.eydis.springtide"].Effects);
        Assert.Equal(120, catalog.AbilitiesById["ability.creature.eydis.springtide"].CooldownTicks);
        Assert.Equal(1f, springtide.ScalingCoefficient);
        Assert.Equal("status.eydis.abundance", springtide.ScalingStatusId);
        Assert.Equal(AttributeType.Power, springtide.StatusScalingAttribute);
        Assert.Equal(0.2f, springtide.StatusScalingCoefficient);
        Assert.Equal(AbilityTargetSelector.AllEnemies, springtide.Target);

        var heartwood = catalog.AbilitiesById["ability.creature.eydis.ancient_heartwood"];
        Assert.Equal(200, heartwood.CooldownTicks);
        Assert.Equal([AttributeType.Armor, AttributeType.Resistance], heartwood.Effects.Select(x => x.Attribute));
        Assert.All(heartwood.Effects, effect =>
        {
            Assert.Equal(0.5f, effect.ScalingCoefficient);
            Assert.Equal(100, effect.DurationTicks);
        });

        var tranquil = catalog.AbilitiesById["ability.creature.eydis.tranquil_waters"];
        Assert.Equal(150, tranquil.CooldownTicks);
        Assert.Equal(
            [StandardConditionType.Slow, StandardConditionType.Weaken],
            tranquil.Effects.Select(x => x.Condition));
        Assert.All(tranquil.Effects, effect => Assert.Equal(AbilityTargetSelector.AllEnemies, effect.Target));

        var endless = catalog.AbilitiesById["ability.creature.eydis.endless_spring"];
        var interval = Assert.Single(endless.Triggers);
        Assert.Equal(100, interval.InitialDelayTicks);
        Assert.Equal(100, interval.InternalCooldownTicks);
        Assert.Equal(
            [
                "effect.creature.eydis.endless_spring.abundance",
                "effect.creature.eydis.endless_spring.heal"
            ],
            interval.EffectIds);
        var heal = endless.Effects[1];
        Assert.Equal("status.eydis.abundance", heal.ScalingStatusId);
        Assert.Equal(AttributeType.MaxHealth, heal.StatusScalingAttribute);
        Assert.Equal(0.01f, heal.StatusScalingCoefficient);

        var abundance = catalog.StatusesById["status.eydis.abundance"];
        Assert.Equal(AbilityStatusStackingPolicy.Stack, abundance.StackingPolicy);
        Assert.Equal(60, abundance.MaxStacks);
        Assert.Equal(0, abundance.DurationTicks);
    }

    [Fact]
    public void Springtide_gains_twenty_percent_power_scaling_per_abundance()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var springtide = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.eydis.springtide"]);

        int DamageWithStacks(int stacks)
        {
            var eydis = CreateCombatant($"eydis-{stacks}", CombatTeam.Friendly, [springtide]);
            var enemy = CreateCombatant($"enemy-{stacks}", CombatTeam.Hostile, [], maxHealth: 1_000);
            if (stacks > 0)
                eydis.Statuses.Add(new RuntimeStatus(statuses["status.eydis.abundance"], eydis, eydis, stacks));
            var engine = new FastCombatEngine(
                statuses,
                new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));
            var result = engine.Run([eydis], [enemy]);
            return Assert.Single(result.EventLog, log =>
                log.Source == "effect.creature.eydis.springtide.damage").Magnitude;
        }

        var baseDamage = DamageWithStacks(0);
        var stackedDamage = DamageWithStacks(3);
        Assert.True(stackedDamage > baseDamage);
        Assert.InRange((double)stackedDamage / baseDamage, 1.55, 1.65);
    }

    [Fact]
    public void Endless_spring_gains_abundance_before_healing_one_percent_max_health_per_stack()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var endless = AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.eydis.endless_spring"]);
        var eydis = CreateCombatant("eydis", CombatTeam.Friendly, [endless], maxHealth: 1_000);
        eydis.SetHealth(500);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 100_000);
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 201, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([eydis], [enemy]);

        Assert.Equal(2, eydis.GetStatusStacks("status.eydis.abundance"));
        Assert.Equal(530, eydis.Health);
        Assert.Equal(
            [10, 20],
            result.EventLog
                .Where(log => log.Source == "effect.creature.eydis.endless_spring.heal" && log.EventType == EventType.Heal)
                .Select(log => log.Magnitude));
    }

    [Fact]
    public void Ancient_heartwood_expires_and_tranquil_waters_debuffs_every_enemy()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var abilities = AbilityCompiler.CompileAbilities(
            [
                catalog.AbilitiesById["ability.creature.eydis.ancient_heartwood"],
                catalog.AbilitiesById["ability.creature.eydis.tranquil_waters"]
            ]);
        var eydis = new RuntimeCombatant(
            "eydis",
            "Eydis",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100_000,
                [AttributeType.Power] = 50,
                [AttributeType.Armor] = 100,
                [AttributeType.Resistance] = 80,
                [AttributeType.AttackSpeed] = 0
            },
            abilities.Values,
            ["Role.Test"]);
        var first = CreateCombatant("first", CombatTeam.Hostile, [], maxHealth: 100_000);
        var second = CreateCombatant("second", CombatTeam.Hostile, [], maxHealth: 100_000);
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 101, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([eydis], [first, second]);

        Assert.Equal(100, eydis.GetAttribute(AttributeType.Armor));
        Assert.Equal(80, eydis.GetAttribute(AttributeType.Resistance));
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.eydis.ancient_heartwood.defense"
            && log.EventType == EventType.Buff
            && log.Magnitude == 50);
        var tranquil = abilities["ability.creature.eydis.tranquil_waters"];
        var tranquilEydis = CreateCombatant("tranquil-eydis", CombatTeam.Friendly, [tranquil]);
        var tranquilFirst = CreateCombatant("tranquil-first", CombatTeam.Hostile, []);
        var tranquilSecond = CreateCombatant("tranquil-second", CombatTeam.Hostile, []);
        var tranquilEngine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        tranquilEngine.Run([tranquilEydis], [tranquilFirst, tranquilSecond]);

        Assert.All(new[] { tranquilFirst, tranquilSecond }, enemy =>
        {
            Assert.Equal(1, enemy.GetConditionStacks(StandardConditionType.Slow));
            Assert.Equal(1, enemy.GetConditionStacks(StandardConditionType.Weaken));
        });
    }

    [Fact]
    public void Json_catalog_mad_king_authors_the_requested_floor_ten_kit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var catalog = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options).GetCatalog();
        var profile = new JsonCreatureAbilityDefinitionProvider(CreateConfig(), contentRoot, options);

        Assert.Equal(
            [
                "ability.creature.mad_king.bloodbath",
                "ability.creature.mad_king.kings_cleaver",
                "ability.creature.mad_king.unrestrained",
                "ability.creature.mad_king.bloodlust"
            ],
            profile.GetAbilityIds("monster.the_mad_king"));

        var bloodbath = catalog.AbilitiesById["ability.creature.mad_king.bloodbath"];
        var bloodbathDamage = Assert.Single(bloodbath.Effects);
        Assert.Equal(150, bloodbath.CooldownTicks);
        Assert.Equal(AbilityTargetSelector.AllEnemies, bloodbathDamage.Target);
        Assert.Equal(0.4f, bloodbathDamage.ScalingCoefficient);
        Assert.Equal(5, bloodbathDamage.RepeatCount);
        Assert.Equal(20, bloodbathDamage.LifeStealPercentage);

        var cleaver = catalog.AbilitiesById["ability.creature.mad_king.kings_cleaver"];
        Assert.Equal(100, cleaver.CooldownTicks);
        Assert.Equal(2, cleaver.Effects.Count);
        Assert.All(cleaver.Effects, effect => Assert.Equal(3f, effect.ScalingCoefficient));
        Assert.Equal(AbilityTargetSelector.HighestHealthEnemy, cleaver.Effects[0].Target);
        Assert.Equal(AbilityTargetSelector.EventTarget, cleaver.Effects[1].Target);

        var unrestrained = catalog.AbilitiesById["ability.creature.mad_king.unrestrained"];
        Assert.Equal(180, unrestrained.CooldownTicks);
        Assert.Equal(
            [AbilityEffectOperation.ModifyDamageDealt, AbilityEffectOperation.ModifyDamageTaken],
            unrestrained.Effects.Select(effect => effect.Operation));
        Assert.All(unrestrained.Effects, effect =>
        {
            Assert.Equal(40, effect.BaseValue);
            Assert.Equal(100, effect.DurationTicks);
        });

        var bloodlust = catalog.AbilitiesById["ability.creature.mad_king.bloodlust"];
        var bloodlustEffect = Assert.Single(bloodlust.Effects);
        Assert.Equal(AbilitySpecKind.Passive, bloodlust.Kind);
        Assert.Equal(AbilityEffectOperation.SynchronizeAttributePerMissingHealthStep, bloodlustEffect.Operation);
        Assert.Equal(AttributeType.LifeSteal, bloodlustEffect.Attribute);
        Assert.Equal(5, bloodlustEffect.BaseValue);
        Assert.Equal(10, bloodlustEffect.HealthStepPercent);
    }

    [Fact]
    public void Bloodbath_hits_every_enemy_five_times_and_heals_twenty_percent_per_hit()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var bloodbath = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.mad_king.bloodbath"]);
        var madKing = CreateCombatant("mad-king", CombatTeam.Friendly, [bloodbath]);
        madKing.SetHealth(100);
        var first = CreateCombatant("first", CombatTeam.Hostile, [], maxHealth: 1_000);
        var second = CreateCombatant("second", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([madKing], [first, second]);

        var damage = result.EventLog
            .Where(log => log.Source == "effect.creature.mad_king.bloodbath.damage"
                          && log.EventType == EventType.Damage)
            .ToArray();
        Assert.Equal(10, damage.Length);
        Assert.Equal(5, damage.Count(log => log.TargetId == first.Id));
        Assert.Equal(5, damage.Count(log => log.TargetId == second.Id));
        var expectedHealing = damage.Sum(log => (int)Math.Round(log.Magnitude * 0.2f));
        var actualHealing = result.EventLog
            .Where(log => log.Source == "effect.creature.mad_king.bloodbath.damage"
                          && log.EventType == EventType.Heal)
            .Sum(log => log.Magnitude);
        Assert.Equal(expectedHealing, actualHealing);
    }

    [Theory]
    [InlineData(800, 2)]
    [InlineData(500, 1)]
    public void Kings_cleaver_locks_the_highest_health_target_and_doubles_only_above_half(
        int selectedHealth,
        int expectedHits)
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var cleaver = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.mad_king.kings_cleaver"]);
        var madKing = CreateCombatant("mad-king", CombatTeam.Friendly, [cleaver]);
        var selected = CreateCombatant("selected", CombatTeam.Hostile, [], maxHealth: 1_000);
        selected.SetHealth(selectedHealth);
        var lower = CreateCombatant("lower", CombatTeam.Hostile, [], maxHealth: 1_000);
        lower.SetHealth(selectedHealth - 100);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([madKing], [lower, selected]);

        var damage = result.EventLog
            .Where(log => log.Source.StartsWith("effect.creature.mad_king.kings_cleaver", StringComparison.Ordinal)
                          && log.EventType == EventType.Damage)
            .ToArray();
        Assert.Equal(expectedHits, damage.Length);
        Assert.All(damage, log => Assert.Equal(selected.Id, log.TargetId));
    }

    [Fact]
    public void Unrestrained_increases_all_damage_dealt_and_taken_then_expires()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var unrestrained = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.mad_king.unrestrained"]);

        RuntimeCombatant Run(int maxTicks)
        {
            var madKing = CreateCombatant($"mad-king-{maxTicks}", CombatTeam.Friendly, [unrestrained]);
            var enemy = CreateCombatant($"enemy-{maxTicks}", CombatTeam.Hostile, [], maxHealth: 100_000);
            var engine = new FastCombatEngine(
                AbilityCompiler.CompileStatuses(catalog.Statuses),
                new FastCombatEngineOptions(MaxTicks: maxTicks, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));
            engine.Run([madKing], [enemy]);
            return madKing;
        }

        var active = Run(1);
        Assert.Equal(40, active.GetDamageDealtPercent(DamageType.Physical));
        Assert.Equal(40, active.GetDamageDealtPercent(DamageType.Magical));
        Assert.Equal(40, active.GetDamageTakenPercent(DamageType.Physical, active));
        Assert.Equal(40, active.GetDamageTakenPercent(DamageType.Magical, active));
        var expired = Run(101);
        Assert.Equal(0, expired.GetDamageDealtPercent(DamageType.Physical));
        Assert.Equal(0, expired.GetDamageDealtPercent(DamageType.Magical));
        Assert.Equal(0, expired.GetDamageTakenPercent(DamageType.Physical, expired));
        Assert.Equal(0, expired.GetDamageTakenPercent(DamageType.Magical, expired));
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(181, 0)]
    [InlineData(180, 5)]
    [InlineData(100, 25)]
    [InlineData(20, 45)]
    public void Bloodlust_synchronizes_lifesteal_at_each_missing_health_step(
        int health,
        int expectedLifeSteal)
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var bloodlust = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.mad_king.bloodlust"]);
        var madKing = CreateCombatant("mad-king", CombatTeam.Friendly, [bloodlust]);
        madKing.SetHealth(health);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 100_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([madKing], [enemy]);

        Assert.Equal(expectedLifeSteal, madKing.GetAttribute(AttributeType.LifeSteal));
    }

    [Fact]
    public void Bloodlust_removes_lifesteal_steps_after_healing()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var bloodlust = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.mad_king.bloodlust"]);
        var heal = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.mad-king-heal",
            Kind = AbilitySpecKind.Active,
            Name = "Mad King Test Heal",
            Effects =
            [
                new()
                {
                    Id = "effect.test.mad-king-heal",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 50,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        });
        var madKing = CreateCombatant("mad-king", CombatTeam.Friendly, [heal, bloodlust]);
        madKing.SetHealth(100);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 100_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([madKing], [enemy]);

        Assert.Equal(150, madKing.Health);
        Assert.Equal(10, madKing.GetAttribute(AttributeType.LifeSteal));
    }

    [Fact]
    public void Kharads_crushing_verdict_targets_highest_max_health()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var crushingVerdict = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.kharad.crushing_verdict"]);
        var kharad = CreateCombatant("kharad", CombatTeam.Friendly, [crushingVerdict]);
        var smaller = CreateCombatant("smaller", CombatTeam.Hostile, [], maxHealth: 500);
        var larger = CreateCombatant("larger", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        engine.Run([kharad], [smaller, larger]);

        Assert.Equal(500, smaller.Health);
        Assert.True(larger.Health < 1_000);
        Assert.Equal(2, larger.GetConditionStacks(StandardConditionType.Vulnerable));
    }

    [Fact]
    public void Kharads_surviving_inert_pillars_grant_stack_synchronized_resonance()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var selectedIds = new[]
        {
            "ability.creature.kharad.raise_the_twin_pillars",
            "ability.creature.kharad.keystone_resonance"
        };
        var abilities = AbilityCompiler.CompileAbilities(
            selectedIds.Select(id => catalog.AbilitiesById[id]));
        var kharad = CreateCombatant(
            "kharad",
            CombatTeam.Friendly,
            selectedIds.Select(id => abilities[id]),
            maxHealth: 1_000);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 120, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([kharad], [enemy]);

        Assert.Equal(2, kharad.GetStatusStacks("status.kharad.resonance"));
        Assert.Equal(57, kharad.GetAttribute(AttributeType.Power));
        Assert.Equal(10, kharad.GetAttribute(AttributeType.AttackSpeed));
        Assert.Equal(6, kharad.GetAttribute(AttributeType.DamageReduction));
        Assert.DoesNotContain(result.EventLog, log =>
            log.ActorId.Contains(":summon:", StringComparison.Ordinal)
            && log.EventType == EventType.AbilityUse
            && log.Source == "Basic Attack");
    }

    [Fact]
    public void Destroying_both_of_Kharads_pillars_removes_one_resonance_at_group_resolution()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var selectedIds = new[]
        {
            "ability.creature.kharad.raise_the_twin_pillars",
            "ability.creature.kharad.keystone_resonance"
        };
        var abilities = AbilityCompiler.CompileAbilities(
            selectedIds.Select(id => catalog.AbilitiesById[id]));
        var destroyPillars = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.kharad.destroy_pillars",
            Kind = AbilitySpecKind.Active,
            Name = "Destroy Pillars",
            CooldownTicks = 1_000,
            Effects =
            [
                new()
                {
                    Id = "effect.test.kharad.destroy_pillars",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.SummonedEnemies,
                    BaseValue = 10_000,
                    AttackType = AttackType.Ranged,
                    DamageType = DamageType.Magical
                }
            ]
        });
        var kharad = CreateCombatant(
            "kharad",
            CombatTeam.Friendly,
            selectedIds.Select(id => abilities[id]),
            maxHealth: 1_000);
        kharad.Statuses.Add(new RuntimeStatus(
            statuses["status.kharad.resonance"], kharad, kharad, stacks: 1));
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [destroyPillars], maxHealth: 10_000);
        var engine = new FastCombatEngine(
            statuses,
            AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 120, BasicAttackIntervalTicks: 1_000));

        engine.Run([kharad], [enemy]);

        Assert.Equal(0, kharad.GetStatusStacks("status.kharad.resonance"));
    }

    [Fact]
    public void Kharads_seal_stops_pulsing_when_broken_and_rewards_natural_expiration()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var seal = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.kharad.seal_of_ascension"]);
        var resonance = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.kharad.keystone_resonance"]);

        var expiringKharad = CreateCombatant("expiring-kharad", CombatTeam.Friendly, [seal, resonance]);
        var durableEnemy = CreateCombatant("durable-enemy", CombatTeam.Hostile, [], maxHealth: 10_000);
        var expirationEngine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 100, BasicAttackIntervalTicks: 1_000));
        var expirationResult = expirationEngine.Run([expiringKharad], [durableEnemy]);

        Assert.Equal(1, expiringKharad.GetStatusStacks("status.kharad.resonance"));
        Assert.Equal(5, expirationResult.EventLog.Count(log =>
            log.Source == "effect.creature.kharad.seal_of_ascension.pulse"
            && log.EventType is EventType.Damage or EventType.DamageCrit));

        var breaker = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.kharad.break_seal",
            Kind = AbilitySpecKind.Active,
            Name = "Break Seal",
            Effects =
            [
                new()
                {
                    Id = "effect.test.kharad.break_seal",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        });
        var brokenKharad = CreateCombatant("broken-kharad", CombatTeam.Friendly, [seal, resonance]);
        brokenKharad.Statuses.Add(new RuntimeStatus(
            statuses["status.kharad.resonance"], brokenKharad, brokenKharad, stacks: 1));
        var attackingEnemy = CreateCombatant("attacking-enemy", CombatTeam.Hostile, [breaker]);
        var breakEngine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 25, BasicAttackIntervalTicks: 1_000));
        var breakResult = breakEngine.Run([brokenKharad], [attackingEnemy]);

        Assert.Equal(0, brokenKharad.GetStatusStacks("status.kharad.resonance"));
        Assert.DoesNotContain(breakResult.EventLog, log =>
            log.Source == "effect.creature.kharad.seal_of_ascension.pulse"
            && log.EventType is EventType.Damage or EventType.DamageCrit);
    }

    [Fact]
    public void Kharads_resonance_cannot_be_reduced_after_reaching_five_stacks()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var definition = AbilityCompiler.CompileStatuses(catalog.Statuses)["status.kharad.resonance"];
        var kharad = CreateCombatant("kharad", CombatTeam.Friendly, []);
        var status = new RuntimeStatus(definition, kharad, kharad, stacks: 4);

        status.AddStacks(1);
        status.AddStacks(-3);

        Assert.Equal(5, status.Stacks);
        Assert.True(status.IsRemovalLocked);
    }

    [Fact]
    public void Vaelors_mirror_lance_targets_current_health_extremes_and_consumes_both_charges()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var mirrorLance = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.vaelor.mirror_lance"]);
        var vaelor = CreateCombatant("vaelor", CombatTeam.Friendly, [mirrorLance]);
        vaelor.Statuses.Add(new RuntimeStatus(
            statuses["status.vaelor.next_physical_damage"], vaelor, vaelor, stacks: 10));
        vaelor.Statuses.Add(new RuntimeStatus(
            statuses["status.vaelor.next_magical_damage"], vaelor, vaelor, stacks: 20));
        var highestCurrentHealth = CreateCombatant("highest", CombatTeam.Hostile, [], maxHealth: 1_000);
        highestCurrentHealth.AdjustHealth(-200);
        var lowestCurrentHealth = CreateCombatant("lowest", CombatTeam.Hostile, [], maxHealth: 300);
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 17));

        var result = engine.Run([vaelor], [highestCurrentHealth, lowestCurrentHealth]);

        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.vaelor.mirror_lance.physical"
            && log.TargetId == "highest"
            && log.EventType is EventType.Damage or EventType.DamageCrit);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.vaelor.mirror_lance.magical"
            && log.TargetId == "lowest"
            && log.EventType is EventType.Damage or EventType.DamageCrit);
        Assert.Equal(0, vaelor.GetStatusStacks("status.vaelor.next_physical_damage"));
        Assert.Equal(0, vaelor.GetStatusStacks("status.vaelor.next_magical_damage"));
    }

    [Fact]
    public void Vaelors_reflective_mirrorplate_returns_all_direct_health_damage()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var mirrorplate = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.vaelor.reflective_mirrorplate"]);
        var directAttack = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.vaelor.direct_attack",
            Kind = AbilitySpecKind.Active,
            Name = "Direct Attack",
            Effects =
            [
                new()
                {
                    Id = "effect.test.vaelor.direct_attack",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 40,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        });
        var vaelor = CreateCombatant("vaelor", CombatTeam.Friendly, [mirrorplate]);
        var attacker = CreateCombatant("attacker", CombatTeam.Hostile, [directAttack]);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([vaelor], [attacker]);

        Assert.Equal(160, vaelor.Health);
        Assert.Equal(160, attacker.Health);
        Assert.Contains(result.EventLog, log =>
            log.Source == "condition.thorns"
            && log.TargetId == "attacker"
            && log.EventType == EventType.ReflectedDamage
            && log.Magnitude == 40);
    }

    [Fact]
    public void Vaelors_mirrorbound_charges_the_opposite_damage_types()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var mirrorbound = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.vaelor.mirrorbound"]);
        var attacks = new[] { DamageType.Physical, DamageType.Magical }
            .Select(damageType => AbilityCompiler.CompileAbility(new AbilitySpec
            {
                Id = $"ability.test.vaelor.{damageType.ToString().ToLowerInvariant()}",
                Kind = AbilitySpecKind.Active,
                Name = $"{damageType} Attack",
                Effects =
                [
                    new()
                    {
                        Id = $"effect.test.vaelor.{damageType.ToString().ToLowerInvariant()}",
                        Operation = AbilityEffectOperation.Damage,
                        Target = AbilityTargetSelector.CurrentTarget,
                        BaseValue = 10,
                        AttackType = AttackType.Ranged,
                        DamageType = damageType
                    }
                ]
            }))
            .ToArray();
        var vaelor = CreateCombatant("vaelor", CombatTeam.Friendly, [mirrorbound]);
        var attacker = CreateCombatant("attacker", CombatTeam.Hostile, attacks);
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        engine.Run([vaelor], [attacker]);

        Assert.Equal(1, vaelor.GetStatusStacks("status.vaelor.next_magical_damage"));
        Assert.Equal(1, vaelor.GetStatusStacks("status.vaelor.next_physical_damage"));
    }

    [Fact]
    public void Garrans_toll_transfers_fifteen_percent_power_for_the_rest_of_combat()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.creature.garran.gatekeepers_toll"]]);
        var garran = CreateCombatant("garran", CombatTeam.Friendly, abilities.Values);
        var target = CreateCombatant("target", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([garran], [target]);

        Assert.Equal(58, garran.GetAttribute(AttributeType.Power));
        Assert.Equal(42, target.GetAttribute(AttributeType.Power));
        Assert.Contains(
            result.EventLog,
            log => log.Source == "effect.creature.garran.gatekeepers_toll.transfer"
                   && log.EventType == EventType.Debuff
                   && log.Magnitude == 8);
    }

    [Fact]
    public void Garrans_gate_seals_shatter_at_each_quarter_health_threshold()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.creature.garran.the_first_gate"]]);
        var garran = new RuntimeCombatant(
            "garran",
            "Garran, the Gatekeeper",
            CombatTeam.Hostile,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 2_000,
                [AttributeType.Power] = 1,
                [AttributeType.Armor] = 100,
                [AttributeType.Resistance] = 100
            },
            abilities.Values);
        var thresholdStrike = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.garran.threshold_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Threshold Strike",
            CooldownTicks = 1,
            Effects =
            [
                new()
                {
                    Id = "effect.test.garran.threshold_strike",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 500,
                    DamageType = DamageType.None
                }
            ]
        });
        var attacker = new RuntimeCombatant(
            "attacker",
            "Attacker",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 10_000,
                [AttributeType.Power] = 1
            },
            [thresholdStrike]);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1000, RandomSeed: 17));

        var result = engine.Run([attacker], [garran]);

        Assert.Equal(500, garran.Health);
        Assert.Equal(100, garran.GetAttribute(AttributeType.Armor));
        Assert.Equal(100, garran.GetAttribute(AttributeType.Resistance));
        Assert.Equal(0, garran.GetStatusStacks("status.garran.gate_seal"));
        Assert.Equal(
            3,
            result.EventLog.Count(log =>
                log.Source.StartsWith("effect.creature.garran.the_first_gate.shatter_", StringComparison.Ordinal)
                && log.Source.EndsWith("_armor", StringComparison.Ordinal)
                && log.EventType == EventType.Debuff
                && log.Magnitude == -15));
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
    public void Json_catalog_does_not_expose_unimplemented_authored_cost_notes()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var sniperStrike = catalog.AbilitiesById["ability.creature.goblin_archer.snipers_strike"];

        Assert.Equal(
            "Deal 135% Physical Damage with +50% Critical Chance.",
            sniperStrike.Description);
        Assert.DoesNotContain(
            catalog.AbilitiesById.Values,
            ability => ability.Description.Contains("resource unspecified", StringComparison.OrdinalIgnoreCase));
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
            "poisonous_rat", "rotfly_toad", "brown_slime", "cave_bat", "giant_bat", "undead",
            "gnoll_pack_leader", "gnoll_raider", "gnoll_shaman", "kobold_skirmisher", "kobold_sorcerer",
            "feral_ghoul", "plague_ghoul", "ravenous_ghoul", "vampire_fledgeling", "wandering_ghost",
            "web_weaver_spider", "spider_queen", "bark_golem", "treant_guardian", "elder_treant"
        };

        var allAbilityIds = monsterIds
            .Select(id => (MonsterId: $"monster.{id}", AbilityIds: profiles.GetAbilityIds($"monster.{id}")))
            .ToArray();

        Assert.Equal(67, allAbilityIds.Length);
        Assert.All(allAbilityIds, profile =>
            Assert.Equal(profile.MonsterId is "monster.hobgoblin" or "monster.spider_queen" or "monster.elder_treant" ? 3 : 2, profile.AbilityIds.Count));
        Assert.Equal(137, allAbilityIds.SelectMany(x => x.AbilityIds).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(180, catalog.AbilitiesById.Count);
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

        Assert.Equal(70, allDefinitions.Count);
        Assert.Equal(67, allDefinitions.Select(x => x.SourceMonsterId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(67, allLootTables.Count);
        Assert.Equal(70, essenceItems.Count);
        Assert.All(allDefinitions, definition =>
        {
            Assert.StartsWith("monster.", definition.SourceMonsterId, StringComparison.Ordinal);
            Assert.StartsWith("ability.creature.", definition.ActiveAbilityId, StringComparison.Ordinal);
            Assert.StartsWith("ability.creature.", definition.PassiveAbilityId, StringComparison.Ordinal);
        });
        Assert.Equal(2, allDefinitions.Count(x => x.SourceMonsterId == "monster.hobgoblin"));
        Assert.Equal(2, allDefinitions.Count(x => x.SourceMonsterId == "monster.spider_queen"));
        Assert.Equal(2, allDefinitions.Count(x => x.SourceMonsterId == "monster.elder_treant"));
    }

    [Fact]
    public void Authored_ability_tags_are_available_to_backend_interactions()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();

        var sonicScreech = catalog.AbilitiesById["ability.creature.cave_bat.sonic_screech"];
        var echolocation = catalog.AbilitiesById["ability.creature.cave_bat.echolocation"];
        var acidSplash = catalog.AbilitiesById["ability.creature.green_slime.acid_splash"];
        var spiderEyes = catalog.AbilitiesById["ability.creature.spider.spider_eyes"];
        var poisonedArrows = catalog.AbilitiesById["ability.creature.goblin_archer.poisoned_arrows"];
        var corrosiveOoze = catalog.AbilitiesById["ability.creature.green_slime.corrosive_ooze"];

        Assert.Equal(["Magical", "Ranged", "Debuff"], sonicScreech.Tags);
        Assert.Equal(["Buff"], echolocation.Tags);
        Assert.Equal(["Magical", "Ranged", "Poison", "Area"], acidSplash.Tags);
        Assert.Equal(["Buff"], spiderEyes.Tags);
        Assert.Contains(sonicScreech.Id, catalog.AbilityIdsByTag["Debuff"]);
        Assert.Contains(echolocation.Id, catalog.AbilityIdsByTag["Buff"]);
        Assert.Contains(acidSplash.Id, catalog.AbilityIdsByTag["Poison"]);
        Assert.False(catalog.AbilityIdsByTag.ContainsKey("Damage"));
        Assert.Equal(
            "Ranged attacks have a 10% chance to apply Poison(10).",
            poisonedArrows.Description);
        Assert.Equal(
            "When damaged by a Physical melee attack, apply Poison(3) to the attacker.",
            corrosiveOoze.Description);
    }

    [Fact]
    public void Multi_essence_creatures_have_distinct_variant_names_and_display_names()
    {
        var definitions = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator()).GetAll();
        var multiEssenceCreatures = definitions
            .GroupBy(x => x.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();

        Assert.Equal(3, multiEssenceCreatures.Count);
        Assert.All(multiEssenceCreatures, variants =>
        {
            Assert.Single(variants.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase));
            Assert.Equal(variants.Count(), variants.Select(x => x.VariantName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(variants, essence =>
            {
                Assert.False(string.IsNullOrWhiteSpace(essence.VariantName));
                Assert.Equal($"{essence.Name} — {essence.VariantName}", essence.DisplayName);
            });
        });
        var hobgoblin = multiEssenceCreatures.Single(group => group.Key == "monster.hobgoblin");
        Assert.Contains(hobgoblin, x => x.DisplayName == "Hobgoblin Essence — Intimidating Slam");
        Assert.Contains(hobgoblin, x => x.DisplayName == "Hobgoblin Essence — Brutal Charge");
        var spiderQueen = multiEssenceCreatures.Single(group => group.Key == "monster.spider_queen");
        Assert.Contains(spiderQueen, x => x.DisplayName == "Spider Queen Essence — Webbed Domain");
        Assert.Contains(spiderQueen, x => x.DisplayName == "Spider Queen Essence — Royal Venom");
        var elderTreant = multiEssenceCreatures.Single(group => group.Key == "monster.elder_treant");
        Assert.Contains(elderTreant, x => x.DisplayName == "Elder Treant Essence — Ancient Sap");
        Assert.Contains(elderTreant, x => x.DisplayName == "Elder Treant Essence — Thornstorm");
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
        AddStandardCondition(ally, ally, StandardConditionType.Taunt);
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
        AddStandardCondition(foxOwner, foxOwner, StandardConditionType.Taunt);
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
    public void Engine_converts_hard_control_into_boss_stagger_and_captures_transition_frames()
    {
        var staggerAbility = new AbilitySpec
        {
            Id = "ability.test.stagger",
            Kind = AbilitySpecKind.Active,
            Name = "Stagger Test",
            CooldownTicks = 100,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.test.stagger"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.test.stagger",
                    Operation = AbilityEffectOperation.ApplyCondition,
                    Target = AbilityTargetSelector.CurrentTarget,
                    Condition = StandardConditionType.Stun,
                    BaseValue = 2,
                    GuaranteedConditionApplication = true,
                    StaggerPower = 100
                }
            ]
        };
        var compiled = AbilityCompiler.CompileAbility(staggerAbility);
        var friendly = CreateCombatant(
            "friendly",
            CombatTeam.Friendly,
            [compiled],
            canBasicAttack: false);
        var hostile = CreateCombatant(
            "boss",
            CombatTeam.Hostile,
            [],
            canBasicAttack: false,
            staggerDefinition: new BossStaggerDefinition
            {
                Enabled = true,
                BaseThreshold = 100,
                BreakDurationTicks = 3,
                RecoveryDurationTicks = 2,
                ThresholdGrowthPercentPerBreak = 50
            });
        var checkpoints = new List<CombatCheckpoint>();
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 5, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([friendly], [hostile], checkpointObserver: checkpoints.Add, checkpointIntervalTicks: 10);

        Assert.False(hostile.HasCondition(StandardConditionType.Stun));
        Assert.Contains(result.EventLog, item => item.EventType == EventType.StaggerApplied && item.Magnitude == 100);
        Assert.Contains(result.EventLog, item => item.EventType == EventType.StaggerBroken);
        Assert.Contains(result.EventLog, item => item.EventType == EventType.StaggerRecovered);
        Assert.Equal(100, result.EntityStats.Single(x => x.EntityId == "friendly").StaggerContributed);
        Assert.Equal(1, result.EntityStats.Single(x => x.EntityId == "friendly").StaggerBreaks);
        Assert.Contains(checkpoints, checkpoint => checkpoint.Hostile.Single().IsStaggered);
        Assert.Contains(checkpoints, checkpoint =>
            !checkpoint.Hostile.Single().IsStaggered
            && checkpoint.Hostile.Single().IsStaggerRecovering
            && checkpoint.Hostile.Single().MaxStagger == 150);
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
    public void Self_health_catalog_passives_ignore_other_combatants_health_changes()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        string[] selfHealthAbilityIds =
        [
            "ability.creature.vampire_bat.erratic_flight",
            "ability.creature.blood_zombie.clotted_flesh",
            "ability.creature.lumo_sentinel.cracked_core",
            "ability.creature.transparent_slime.reconstitute",
            "ability.creature.moss_lizard.lost_tail",
            "ability.creature.hollow_stag.hollow_core",
            "ability.creature.rotroot_shambler.decaying_husk",
            "ability.creature.garran.the_first_gate",
            "ability.creature.mad_king.bloodlust",
            "ability.creature.spider_queen.royal_cocoon"
        ];

        foreach (var abilityId in selfHealthAbilityIds)
        {
            var healthChangedTriggers = catalog.AbilitiesById[abilityId].Triggers
                .Where(trigger => trigger.Event == AbilityTriggerEvent.OnHealthChanged)
                .ToArray();

            Assert.NotEmpty(healthChangedTriggers);
            Assert.All(healthChangedTriggers, trigger =>
                Assert.Contains(
                    trigger.Conditions,
                    condition => condition.Type == AbilityConditionType.EventSourceIsSelf));
        }
    }

    [Fact]
    public void Hollow_core_only_activates_and_generates_threat_for_its_owners_health_changes()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var hollowCoreSpec = catalog.AbilitiesById["ability.creature.hollow_stag.hollow_core"];
        var healthChangedTriggers = hollowCoreSpec.Triggers
            .Where(trigger => trigger.Event == AbilityTriggerEvent.OnHealthChanged)
            .ToArray();
        Assert.Equal(4, healthChangedTriggers.Length);
        Assert.All(healthChangedTriggers, trigger => Assert.Single(trigger.EffectIds));

        var hollowCore = AbilityCompiler.CompileAbility(hollowCoreSpec);
        var selfDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.self_damage",
            Kind = AbilitySpecKind.Active,
            Name = "Self Damage",
            CooldownTicks = 100,
            ThreatValue = 0,
            Effects =
            [
                new()
                {
                    Id = "effect.test.self_damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 1
                }
            ]
        });
        var owner = CreateCombatant(
            "foreign-event-owner",
            CombatTeam.Friendly,
            [hollowCore],
            maxHealth: 1_000,
            canBasicAttack: false);
        owner.SetHealth(150);
        var ally = CreateCombatant(
            "foreign-event-ally",
            CombatTeam.Friendly,
            [selfDamage],
            maxHealth: 1_000,
            canBasicAttack: false);
        var idleHostile = CreateCombatant(
            "foreign-event-hostile",
            CombatTeam.Hostile,
            [],
            maxHealth: 1_000,
            canBasicAttack: false);

        var foreignEventResult = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1_000,
                ThreatHalfLifeSeconds: 0))
            .Run([owner, ally], [idleHostile]);

        Assert.Equal(0, owner.GetAttribute(AttributeType.DamageReduction));
        Assert.DoesNotContain(
            foreignEventResult.EntityStats.SelectMany(stats => stats.Abilities),
            ability => ability.Name == hollowCore.Name);

        var hostileDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.hostile_damage",
            Kind = AbilitySpecKind.Active,
            Name = "Hostile Damage",
            CooldownTicks = 100,
            ThreatValue = 0,
            Effects =
            [
                new()
                {
                    Id = "effect.test.hostile_damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 1
                }
            ]
        });
        var damagedOwner = CreateCombatant(
            "own-event-owner",
            CombatTeam.Friendly,
            [hollowCore],
            maxHealth: 1_000,
            canBasicAttack: false);
        damagedOwner.SetHealth(150);
        var attackingHostile = CreateCombatant(
            "own-event-hostile",
            CombatTeam.Hostile,
            [hostileDamage],
            maxHealth: 1_000,
            canBasicAttack: false);

        var ownEventResult = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(
                MaxTicks: 1,
                BasicAttackIntervalTicks: 1_000,
                ThreatHalfLifeSeconds: 0))
            .Run([damagedOwner], [attackingHostile]);

        Assert.Equal(4, damagedOwner.GetAttribute(AttributeType.DamageReduction));
        var ownOwnerStats = ownEventResult.EntityStats.Single(stats => stats.EntityId == damagedOwner.Id);
        var hollowCoreStats = Assert.Single(ownOwnerStats.Abilities, ability => ability.Name == hollowCore.Name);
        Assert.Equal(4, hollowCoreStats.Uses);
        Assert.Equal(400, hollowCoreStats.TotalThreat);
    }

    [Fact]
    public void Web_weaver_spider_abilities_double_grasp_against_slow_and_track_web_walker_haste()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var weaversGrasp = catalog.AbilitiesById["ability.creature.web_weaver_spider.weavers_grasp"];
        Assert.Equal(
            "Deal 80% Physical Damage. If the target is Slowed, deal another 80% Physical Damage.",
            weaversGrasp.Description);

        var abilities = AbilityCompiler.CompileAbilities(
        [
            weaversGrasp,
            catalog.AbilitiesById["ability.creature.web_weaver_spider.web_walker"]
        ]);

        var unslowedWeaver = CreateCombatant(
            "unslowed-weaver",
            CombatTeam.Friendly,
            [abilities["ability.creature.web_weaver_spider.weavers_grasp"]],
            canBasicAttack: false);
        var unslowedTarget = CreateCombatant("unslowed-target", CombatTeam.Hostile, [], canBasicAttack: false);
        var unslowedResult = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([unslowedWeaver], [unslowedTarget]);

        var slowedWeaver = CreateCombatant(
            "slowed-weaver",
            CombatTeam.Friendly,
            [abilities["ability.creature.web_weaver_spider.weavers_grasp"]],
            canBasicAttack: false);
        var slowedTarget = CreateCombatant("slowed-target", CombatTeam.Hostile, [], canBasicAttack: false);
        AddStandardCondition(slowedTarget, slowedTarget, StandardConditionType.Slow);
        var slowedResult = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([slowedWeaver], [slowedTarget]);

        var unslowedDamage = Assert.Single(
            DamageMagnitudes(unslowedResult, "effect.creature.web_weaver_spider.weavers_grasp.damage"));
        var slowedDamageEvents = slowedResult.EventLog
            .Where(log => log.Source.StartsWith("effect.creature.web_weaver_spider.weavers_grasp", StringComparison.Ordinal)
                          && log.EventType == EventType.Damage)
            .ToArray();
        Assert.Equal(2, slowedDamageEvents.Length);
        Assert.Equal(
            unslowedDamage,
            Assert.Single(slowedDamageEvents, log => log.Source.EndsWith(".damage", StringComparison.Ordinal)).Magnitude);
        Assert.Contains(
            slowedDamageEvents,
            log => log.Source == "effect.creature.web_weaver_spider.weavers_grasp.slowed_damage");

        var walker = CreateCombatant(
            "walker",
            CombatTeam.Friendly,
            [abilities["ability.creature.web_weaver_spider.web_walker"]],
            canBasicAttack: false);
        var webbedEnemy = CreateCombatant("webbed-enemy", CombatTeam.Hostile, [], canBasicAttack: false);
        AddStandardCondition(webbedEnemy, webbedEnemy, StandardConditionType.Slow);
        var walkerResult = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 101, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([walker], [webbedEnemy]);

        Assert.Contains(
            walkerResult.EventLog,
            log => log.Source == "condition.haste" && log.EventType == EventType.StatusEffect);
        Assert.False(walker.HasCondition(StandardConditionType.Haste));
    }

    [Fact]
    public void Spider_queen_abilities_apply_domain_venom_and_complete_royal_cocoon()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var abilities = AbilityCompiler.CompileAbilities(
        [
            catalog.AbilitiesById["ability.creature.spider_queen.webbed_domain"],
            catalog.AbilitiesById["ability.creature.spider_queen.royal_venom"],
            catalog.AbilitiesById["ability.creature.spider_queen.royal_cocoon"]
        ]);

        var domainQueen = CreateCombatant(
            "domain-queen",
            CombatTeam.Friendly,
            [abilities["ability.creature.spider_queen.webbed_domain"]],
            canBasicAttack: false);
        var domainEnemyOne = CreateCombatant("domain-enemy-1", CombatTeam.Hostile, [], canBasicAttack: false);
        var domainEnemyTwo = CreateCombatant("domain-enemy-2", CombatTeam.Hostile, [], canBasicAttack: false);
        new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 11))
            .Run([domainQueen], [domainEnemyOne, domainEnemyTwo]);

        Assert.True(domainQueen.HasCondition(StandardConditionType.Haste));
        Assert.True(domainEnemyOne.HasCondition(StandardConditionType.Slow));
        Assert.True(domainEnemyTwo.HasCondition(StandardConditionType.Slow));

        var venomQueen = CreateCombatant(
            "venom-queen",
            CombatTeam.Friendly,
            [abilities["ability.creature.spider_queen.royal_venom"]]);
        var venomEnemy = CreateCombatant("venom-enemy", CombatTeam.Hostile, [], canBasicAttack: false);
        new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1, RandomSeed: 11))
            .Run([venomQueen], [venomEnemy]);

        Assert.Equal(1, venomQueen.GetStatusStacks("status.spider_queen.royal_venom"));
        Assert.Equal(20, venomEnemy.GetConditionStacks(StandardConditionType.Poison));

        var thresholdStrike = new AbilitySpec
        {
            Id = "ability.test.royal_cocoon_threshold_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Royal Cocoon Threshold Strike",
            CooldownTicks = 1000,
            Effects =
            [
                new()
                {
                    Id = "effect.test.royal_cocoon_threshold_strike.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 14,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        };
        var strike = Assert.Single(AbilityCompiler.CompileAbilities([thresholdStrike]).Values);
        var cocoonQueen = CreateCombatant(
            "cocoon-queen",
            CombatTeam.Friendly,
            [abilities["ability.creature.spider_queen.royal_cocoon"]],
            maxHealth: 1000,
            canBasicAttack: false);
        var attacker = CreateCombatant(
            "cocoon-attacker",
            CombatTeam.Hostile,
            [strike],
            maxHealth: 1000,
            canBasicAttack: false);
        AddStandardCondition(cocoonQueen, cocoonQueen, StandardConditionType.Unstoppable);
        AddStandardCondition(cocoonQueen, cocoonQueen, StandardConditionType.Ward);
        var cocoonResult = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 81, BasicAttackIntervalTicks: 1000, RandomSeed: 11))
            .Run([cocoonQueen], [attacker]);

        var thresholdDamage = Assert.Single(
            cocoonResult.EventLog,
            log => log.Source == "effect.test.royal_cocoon_threshold_strike.damage"
                   && log.EventType == EventType.Damage);
        Assert.Equal(1000 - thresholdDamage.Magnitude + 300, cocoonQueen.Health);
        Assert.False(cocoonQueen.HasCondition(StandardConditionType.Stun));
        Assert.Equal(0, cocoonQueen.GetStatusStacks("status.spider_queen.royal_cocoon"));
        Assert.Contains(
            cocoonResult.EventLog,
            log => log.Source == "condition.stun" && log.EventType == EventType.StatusEffect);
        Assert.Contains(
            cocoonResult.EventLog,
            log => log.Source == "effect.creature.spider_queen.royal_cocoon.heal"
                   && log.EventType == EventType.Heal
                   && log.Magnitude == 300);
    }

    [Fact]
    public void Bark_golem_abilities_slam_all_enemies_and_apply_barkskin_tradeoff()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
        [
            catalog.AbilitiesById["ability.creature.bark_golem.timber_slam"],
            catalog.AbilitiesById["ability.creature.bark_golem.barkskin"]
        ]);

        var golem = new RuntimeCombatant(
            "bark-golem",
            "Bark Golem",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 1000,
                [AttributeType.Power] = 50,
                [AttributeType.Armor] = 100,
                [AttributeType.CritDamage] = 100,
                [AttributeType.AttackSpeed] = 0
            },
            abilities.Values,
            canBasicAttack: false);
        var enemies = new[]
        {
            CreateCombatant("enemy-1", CombatTeam.Hostile, [], maxHealth: 1000, canBasicAttack: false),
            CreateCombatant("enemy-2", CombatTeam.Hostile, [], maxHealth: 1000, canBasicAttack: false)
        };
        var result = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([golem], enemies);

        Assert.Equal(2, result.EventLog.Count(log =>
            log.Source == "effect.creature.bark_golem.timber_slam.damage"
            && log.EventType == EventType.Damage));
        Assert.Equal(130, golem.GetAttribute(AttributeType.Armor));
        Assert.Equal(100, golem.GetDamageTakenPercent(DamageType.Burn, enemies[0]));
    }

    [Fact]
    public void Treant_guardian_abilities_grant_power_barrier_and_stack_regeneration_every_ten_seconds()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
        [
            catalog.AbilitiesById["ability.creature.treant_guardian.rootbound_shield"],
            catalog.AbilitiesById["ability.creature.treant_guardian.forest_vitality"]
        ]);
        var guardian = CreateCombatant(
            "treant-guardian",
            CombatTeam.Friendly,
            abilities.Values,
            maxHealth: 1000,
            canBasicAttack: false);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10000, canBasicAttack: false);

        new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 201, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([guardian], [enemy]);

        Assert.Equal(200, guardian.Barrier);
        Assert.Equal(4, guardian.RegenerationRatePercent);
    }

    [Fact]
    public void Ancient_sap_weakens_on_doom_and_its_magical_damage_activates_ancient_essence()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
        [
            catalog.AbilitiesById["ability.creature.elder_treant.ancient_sap"],
            catalog.AbilitiesById["ability.creature.elder_treant.ancient_essence"]
        ]);
        var elder = CreateCombatant(
            "elder-treant",
            CombatTeam.Friendly,
            abilities.Values,
            maxHealth: 1000,
            canBasicAttack: false);
        elder.AdjustHealth(-500);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 10000, canBasicAttack: false);
        var result = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 151, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([elder], [enemy]);

        Assert.True(enemy.HasCondition(StandardConditionType.Weaken));
        Assert.Equal(505, elder.Health);
        Assert.Contains(result.EventLog, log =>
            log.Source == "condition.doom"
            && log.EventType == EventType.Damage
            && log.DamageType == DamageType.Magical);
        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.creature.elder_treant.ancient_essence.heal"
            && log.EventType == EventType.Heal
            && log.Magnitude == 5);
    }

    [Fact]
    public void Thornstorm_deals_both_damage_types_to_all_enemies_and_ancient_essence_obeys_its_cooldown()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilities = AbilityCompiler.CompileAbilities(
        [
            catalog.AbilitiesById["ability.creature.elder_treant.thornstorm"],
            catalog.AbilitiesById["ability.creature.elder_treant.ancient_essence"]
        ]);
        var elder = CreateCombatant(
            "elder-treant",
            CombatTeam.Friendly,
            abilities.Values,
            maxHealth: 1000,
            canBasicAttack: false);
        elder.AdjustHealth(-500);
        var enemies = new[]
        {
            CreateCombatant("enemy-1", CombatTeam.Hostile, [], maxHealth: 1000, canBasicAttack: false),
            CreateCombatant("enemy-2", CombatTeam.Hostile, [], maxHealth: 1000, canBasicAttack: false)
        };
        var result = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 7))
            .Run([elder], enemies);

        Assert.Equal(2, result.EventLog.Count(log =>
            log.Source == "effect.creature.elder_treant.thornstorm.magical_damage"
            && log.EventType == EventType.Damage));
        Assert.Equal(2, result.EventLog.Count(log =>
            log.Source == "effect.creature.elder_treant.thornstorm.physical_damage"
            && log.EventType == EventType.Damage));
        Assert.Equal(505, elder.Health);
        Assert.Single(result.EventLog, log =>
            log.Source == "effect.creature.elder_treant.ancient_essence.heal"
            && log.EventType == EventType.Heal);
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
    public void Essence_progression_calibration_compares_attribute_only_expected_and_optimized_envelopes()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var config = CreateConfig();
        var catalogProvider = new JsonAbilityCatalogProvider(config, contentRoot, options);
        var essenceRepository = new JsonEssenceDefinitionRepository(
            config,
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var slotUnlocks = new EssenceSlotUnlockService();
        var runner = new EssenceProgressionCalibrationRunner(
            catalogProvider,
            essenceRepository,
            slotUnlocks);
        string[] offensiveEssences =
        [
            "essence.goblin",
            "essence.raven",
            "essence.venomous_snake",
            "essence.goblin_archer",
            "essence.frost_imp",
            "essence.crystal_wisp",
            "essence.grave_hound",
            "essence.skeleton",
            "essence.glade_panther",
            "essence.flame_imp"
        ];
        int[] anchors = [1, 10, 30, 90];
        var scenarios = anchors.Select(level =>
        {
            var slots = slotUnlocks.GetUnlockedSlotCount(level);
            var expectedCount = Math.Max(1, (int)Math.Ceiling(slots * 0.6));
            var playerHealth = 140 + 20 * (level - 1);
            var playerPower = 10 + 0.25f * (level - 1);
            return new EssenceProgressionCalibrationScenario(
                $"anchor-{level}",
                ProgressionPosition: Math.Max(1, level / 5),
                CharacterLevel: level,
                PlayerAttributes: new Dictionary<AttributeType, float>
                {
                    [AttributeType.MaxHealth] = playerHealth,
                    [AttributeType.Power] = playerPower,
                    [AttributeType.CritChance] = 5,
                    [AttributeType.CritDamage] = 100
                },
                TargetAttributes: new Dictionary<AttributeType, float>
                {
                    [AttributeType.MaxHealth] = 100_000_000,
                    [AttributeType.Power] = 0,
                    [AttributeType.DodgeChance] = 0
                },
                Envelopes:
                [
                    new("attributes-only", []),
                    new("minimum", [new(offensiveEssences[0], 0)]),
                    new("expected", offensiveEssences.Take(expectedCount)
                        .Select(id => new EssenceProgressionCalibrationEssence(id, 1)).ToList()),
                    new("optimized", offensiveEssences.Take(slots)
                        .Select(id => new EssenceProgressionCalibrationEssence(id, 3)).ToList())
                ],
                RandomSeeds: [17, 29, 43],
                MaxTicks: 300);
        }).ToList();

        var first = runner.Run(scenarios);
        var second = runner.Run(scenarios);

        Assert.Equal(anchors.Length * 4, first.Results.Count);
        Assert.Equal(first.Results, second.Results);
        Assert.All(anchors, level =>
        {
            var scenarioId = $"anchor-{level}";
            var baseline = first.Results.Single(result =>
                result.ScenarioId == scenarioId && result.EnvelopeId == "attributes-only");
            var optimized = first.Results.Single(result =>
                result.ScenarioId == scenarioId && result.EnvelopeId == "optimized");
            Assert.True(
                optimized.AverageDamageDone > baseline.AverageDamageDone,
                $"{scenarioId}: optimized Essence envelope should exceed attribute-only damage.");
        });
    }

    [Fact]
    public void Essence_progression_calibration_rejects_loadouts_above_the_level_slot_limit()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var config = CreateConfig();
        var runner = new EssenceProgressionCalibrationRunner(
            new JsonAbilityCatalogProvider(config, contentRoot, options),
            new JsonEssenceDefinitionRepository(
                config,
                contentRoot,
                options,
                new EssenceDefinitionValidator()),
            new EssenceSlotUnlockService());
        var scenario = new EssenceProgressionCalibrationScenario(
            "slot-limit",
            1,
            1,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 140 },
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1_000 },
            [
                new("attributes-only", []),
                new("invalid", [new("essence.goblin", 0), new("essence.raven", 0)])
            ],
            [17]);

        var exception = Assert.Throws<InvalidOperationException>(() => runner.Run([scenario]));

        Assert.Contains("exceed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("slots", exception.Message, StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal(140, report.RequiredSlotCount);
        Assert.Equal(70, report.EssenceCount);
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f
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

        Assert.Contains(DamageMagnitudes(result, "effect.damage.main"), magnitude => magnitude is >= 8 and <= 12);
        Assert.Contains(DamageMagnitudes(result, "effect.damage.main.evolved_bonus"), magnitude => magnitude is >= 4 and <= 6);
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f
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
                            ScalingAttribute = AttributeType.Power,
                            ScalingCoefficient = 0.08f
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

        Assert.Contains(DamageMagnitudes(result, "effect.damage.main"), magnitude => magnitude is >= 8 and <= 12);
        Assert.Contains(DamageMagnitudes(result, "effect.damage.evolved"), magnitude => magnitude is >= 3 and <= 5);
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 2f
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

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, ascensionTier: 2);
        Assert.Equal(2.48f, Assert.Single(scaled.Effects).ScalingCoefficient, precision: 3);
        Assert.Contains(DamageMagnitudes(result, "effect.damage.main"), magnitude => magnitude is >= 99 and <= 149);
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 2f
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
                            ScalingAttribute = AttributeType.Power,
                            ScalingCoefficient = 1f
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

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, ascensionTier: 1);
        Assert.Equal(2.24f, Assert.Single(scaled.Effects).ScalingCoefficient, precision: 3);
        Assert.Contains(DamageMagnitudes(result, "effect.damage.main"), magnitude => magnitude is >= 90 and <= 134);
        Assert.Contains(DamageMagnitudes(result, "effect.damage.evolved"), magnitude => magnitude is >= 45 and <= 67);
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
            CooldownTicks = 20,
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f
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
        friendlyCombatant.TemporaryAbilityModifiers.Add(new EssenceAbilityModifierDefinition
        {
            Target = ability.Id,
            Operation = "DelayCooldowns",
            Value = 0.25
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

        Assert.Contains(DamageMagnitudes(result, "effect.damage.main"), magnitude => magnitude is >= 12 and <= 18);
        Assert.Contains(result.EventLog, x => x.Source == "Temporary Modifier Strike" && x.EventType == EventType.AbilityUse);
        var abilityUses = result.EventLog
            .Where(x => x.Source == "Temporary Modifier Strike" && x.EventType == EventType.AbilityUse)
            .Select(x => x.Timestamp)
            .Take(2)
            .ToArray();
        Assert.Equal(2, abilityUses.Length);
        Assert.Equal(25, abilityUses[1] - abilityUses[0]);
    }

    [Fact]
    public async Task Combat_engine_executor_illusion_fox_passive_retaliates_when_holder_is_attacked()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var foxCharacter = CreateSourceCharacter("Illusion Fox Holder");
        var hostileCharacter = CreateSourceCharacter("Hostile Attacker");
        var foxCombatant = CreateCombatEntity("fox-slot", foxCharacter, "essence.illusion_fox");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("fox-slot", foxCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(foxCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), foxCharacter, foxCombatant)],
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
        IncreaseMaxHealth(
            runtime.HostileParticipants.Single().Combatant,
            10_000);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteSimulationAsync(
            runtime,
            new CombatSimulationOptions(
                RandomSeed: 1337,
                MaxTicks: 300,
                StartActiveAbilitiesOnCooldown: true,
                BasicAttackIntervalTicks: 1_000),
            CancellationToken.None);
        var summonLog = result.EventLog.First(x =>
            x.Source == "effect.creature.shadow_imp.shadow_image.summon"
            && x.ActorId == "friendly-slot"
            && x.EventType == EventType.Summon);
        var summonId = summonLog.TargetId;

        Assert.NotNull(summonLog.CombatEntity);
        Assert.Equal("Creature Shadow Image", summonLog.CombatEntity!.Name);
        Assert.Equal("shadow_image", summonLog.CombatEntity.ImagePath);
        Assert.Equal(1, summonLog.CombatEntity.MaxHealth);
        Assert.Equal(1, summonLog.CombatEntity.Health);
        var catalog = provider.GetCatalog();
        Assert.True(catalog.SummonsById.ContainsKey("creatureShadowImage"));
        foreach (var summonIdToVerify in new[] { "shadowImage", "creatureShadowImage" })
        {
            var maxHealth = Assert.Single(
                catalog.SummonsById[summonIdToVerify].Attributes,
                attribute => attribute.Attribute == AttributeType.MaxHealth);
            Assert.Equal(1, maxHealth.BaseValue);
            Assert.Null(maxHealth.ScalingAttribute);
        }
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
        var summonStats = Assert.Single(
            result.EntityStats,
            stats => stats.EntityId == summonId);
        Assert.Equal(0, summonStats.Health);
        Assert.Equal(1, summonStats.MaxHealth);
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
    public void Engine_starts_summon_abilities_on_cooldown_and_new_summons_ready()
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
                MaxTicks: 102,
                BasicAttackIntervalTicks: 1000,
                StartActiveAbilitiesOnCooldown: true));
        var defaultResult = defaultEngine.Run(
            [defaultPair.Friendly],
            [defaultPair.Hostile]);
        var summon = Assert.Single(
            defaultResult.EventLog,
            x => x.EventType == EventType.Summon);

        Assert.Equal(100, summon.Timestamp);
        Assert.Contains(defaultResult.EventLog, x =>
            x.ActorId == summon.TargetId
            && x.Source == "Ready Strike"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == 101);
        Assert.Contains(defaultResult.EventLog, x =>
            x.ActorId == summon.TargetId
            && x.Source == "Basic Attack"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == 101);
        var summonStats = Assert.Single(
            defaultResult.EntityStats,
            stats => stats.EntityId == summon.TargetId);
        Assert.Equal(20, summonStats.Health);
        Assert.Equal(20, summonStats.MaxHealth);
        Assert.Equal(0, summonStats.Barrier);
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
        friendly.AdjustThreat(RuntimeCombatant.BaseThreat);
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
    public async Task Combat_engine_executor_preserves_every_nth_multi_effect_trigger_for_ascended_essence()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.goblin_warrior"],
            [],
            out _,
            out _);
        runtime.FriendlyParticipants.Single()
            .Combatant.EquippedEssences.Single()
            .AscensionTier = 1;
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly-slot");
        var relentless = Assert.Single(friendlyStats.Abilities, x => x.Name == "Relentless");
        var basicAttackCount = CountBasicAttacks(result, "friendly-slot");
        var relentlessBuffs = result.EventLog.Count(x =>
            x.ActorId == "friendly-slot"
            && x.StatsSource == "Relentless"
            && x.EventType == EventType.Buff);

        Assert.True(basicAttackCount >= 3);
        Assert.Equal(basicAttackCount / 3, relentless.Uses);
        Assert.Equal(relentless.Uses, relentlessBuffs);
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

    [Fact]
    public async Task Raid_playback_returns_final_team_health_snapshots()
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, CombatMode.Raid);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var execution = await executor.ExecuteRaidPlaybackAsync(
            runtime,
            checkpointIntervalTicks: 10,
            new CombatSimulationOptions(1337, 6000),
            CancellationToken.None);

        var friendly = Assert.Single(execution.Result.PlayerTeam);
        var hostile = Assert.Single(execution.Result.EnemyTeam);
        Assert.Equal("friendly-slot", friendly.Id);
        Assert.Equal("hostile-slot", hostile.Id);
        Assert.InRange(friendly.Health, 0, friendly.MaxHealth);
        Assert.InRange(hostile.Health, 0, hostile.MaxHealth);
        Assert.NotEmpty(execution.Checkpoints);
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

    [Fact]
    public void Engine_applies_random_three_target_falloff_without_repeating_targets()
    {
        var ability = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.chain.test",
            Kind = AbilitySpecKind.Active,
            Name = "Chain Test",
            Effects =
            [
                new()
                {
                    Id = "effect.chain.test",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.ThreeRandomEnemies,
                    BaseValue = 100,
                    SubsequentTargetDamagePercent = 80,
                    DamageType = DamageType.Magical
                }
            ]
        });
        var caster = CreateCombatant("caster", CombatTeam.Friendly, [ability]);
        var enemies = Enumerable.Range(1, 4)
            .Select(index => CreateCombatant($"enemy-{index}", CombatTeam.Hostile, [], maxHealth: 1_000))
            .ToArray();
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000, RandomSeed: 19));

        var result = engine.Run([caster], enemies);
        var hits = result.EventLog
            .Where(log => log.Source == "effect.chain.test" && log.EventType == EventType.Damage)
            .ToArray();

        Assert.Equal([100, 80, 64], hits.Select(hit => hit.Magnitude));
        Assert.Equal(3, hits.Select(hit => hit.TargetId).Distinct().Count());
    }

    [Fact]
    public void Engine_scales_damage_per_living_non_summoned_ally_excluding_the_caster()
    {
        var ability = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.coordinated.test",
            Kind = AbilitySpecKind.Active,
            Name = "Coordinated Test",
            Effects =
            [
                new()
                {
                    Id = "effect.coordinated.test",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    LivingNonSummonedAllyDamagePercent = 10
                }
            ]
        });
        var caster = CreateCombatant("caster", CombatTeam.Friendly, [ability]);
        var allyOne = CreateCombatant("ally-1", CombatTeam.Friendly, []);
        var allyTwo = CreateCombatant("ally-2", CombatTeam.Friendly, []);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile, [], maxHealth: 1_000);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([caster, allyOne, allyTwo], [enemy]);

        Assert.Contains(result.EventLog, log =>
            log.Source == "effect.coordinated.test"
            && log.EventType == EventType.Damage
            && log.Magnitude == 120);
    }

    [Fact]
    public void Runtime_ability_limits_an_effect_once_per_target()
    {
        var ability = new RuntimeAbility(AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.per-target.test",
            Kind = AbilitySpecKind.Passive,
            Name = "Per Target Test",
            Effects =
            [
                new()
                {
                    Id = "effect.per-target.test",
                    Operation = AbilityEffectOperation.Damage,
                    OncePerTarget = true
                }
            ]
        }));
        var effect = ability.Definition.TriggersByEvent[AbilityTriggerEvent.OnCombatStart].Single().Effects.Single();
        var first = CreateCombatant("first", CombatTeam.Hostile, []);
        var second = CreateCombatant("second", CombatTeam.Hostile, []);

        Assert.True(ability.CanUseEffect(effect, first));
        ability.MarkEffectUsed(effect, first);
        Assert.False(ability.CanUseEffect(effect, first));
        Assert.True(ability.CanUseEffect(effect, second));
    }

    [Fact]
    public void Gnoll_raider_pillages_only_the_first_enemy_to_fall_below_forty_percent_health()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var pillage = AbilityCompiler.CompileAbility(
            catalog.AbilitiesById["ability.creature.gnoll_raider.pillage_the_weak"]);
        var selfDamage = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "ability.test.pillage.self_damage",
            Kind = AbilitySpecKind.Active,
            Name = "Self Damage",
            Effects =
            [
                new()
                {
                    Id = "effect.test.pillage.self_damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 150
                }
            ]
        });
        var firstEnemy = CreateCombatant("enemy-1", CombatTeam.Friendly, [selfDamage]);
        var secondEnemy = CreateCombatant("enemy-2", CombatTeam.Friendly, [selfDamage]);
        var raider = CreateCombatant("raider", CombatTeam.Hostile, [pillage]);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1_000));

        var result = engine.Run([firstEnemy, secondEnemy], [raider]);

        Assert.True(raider.HasCondition(StandardConditionType.Empower));
        Assert.True(raider.HasCondition(StandardConditionType.Haste));
        Assert.Single(result.EventLog, log =>
            log.EventType == EventType.StatusEffect
            && log.Details.Contains("applied Empower", StringComparison.Ordinal));
        Assert.Single(result.EventLog, log =>
            log.EventType == EventType.StatusEffect
            && log.Details.Contains("applied Haste", StringComparison.Ordinal));
    }

    [Fact]
    public void Meran_essence_catalog_authors_requested_cost_and_totem_adjustments()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var packLeader = catalog.AbilitiesById["ability.creature.gnoll_pack_leader.rallying_cry"];
        var raider = catalog.AbilitiesById["ability.creature.gnoll_raider.loot_and_slash"];
        var pillage = catalog.AbilitiesById["ability.creature.gnoll_raider.pillage_the_weak"];
        var skirmisher = catalog.AbilitiesById["ability.creature.kobold_skirmisher.coordinated_assault"];
        var trapMastery = catalog.AbilitiesById["ability.creature.kobold_skirmisher.trap_mastery"];
        var feralPounce = catalog.AbilitiesById["ability.creature.feral_ghoul.feral_pounce"];
        var tasteOfBlood = catalog.AbilitiesById["ability.creature.vampire_fledgeling.taste_of_blood"];
        var freshHunger = catalog.AbilitiesById["ability.creature.vampire_fledgeling.fresh_hunger"];
        var spectralPassage = catalog.AbilitiesById["ability.creature.wandering_ghost.spectral_passage"];
        var ward = catalog.SummonsById["totemicWard"];

        Assert.Equal(140, packLeader.CooldownTicks);
        var rallyingCry = Assert.Single(packLeader.Effects);
        Assert.Equal(AbilityEffectOperation.ApplyCondition, rallyingCry.Operation);
        Assert.Equal(AbilityTargetSelector.RandomAlly, rallyingCry.Target);
        Assert.Equal(StandardConditionType.Empower, rallyingCry.Condition);
        Assert.Empty(raider.Costs);
        Assert.Equal(180, raider.CooldownTicks);
        var raiderDamage = Assert.Single(raider.Effects);
        Assert.Equal(AbilityTargetSelector.ThreeEnemies, raiderDamage.Target);
        Assert.Equal(AttributeType.Power, raiderDamage.ScalingAttribute);
        Assert.Equal(1.3f, raiderDamage.ScalingCoefficient);
        Assert.Equal(
            [StandardConditionType.Empower, StandardConditionType.Haste],
            pillage.Effects.Select(effect => effect.Condition));
        Assert.All(pillage.Effects, effect => Assert.Equal(1, effect.Uses));
        Assert.Empty(skirmisher.Costs);
        var coordinatedDamage = Assert.Single(skirmisher.Effects);
        Assert.Equal(1.8f, coordinatedDamage.ScalingCoefficient);
        Assert.Equal(15, coordinatedDamage.LivingNonSummonedAllyDamagePercent);
        Assert.Empty(trapMastery.Triggers);
        Assert.Empty(trapMastery.Effects);
        Assert.Equal(150, feralPounce.CooldownTicks);
        Assert.Equal(1.5f, feralPounce.Effects.Single(effect => effect.Operation == AbilityEffectOperation.Damage).ScalingCoefficient);
        Assert.Equal(1.75f, Assert.Single(tasteOfBlood.Effects).ScalingCoefficient);
        Assert.Equal(0.35f, Assert.Single(freshHunger.Effects).EventMagnitudeCoefficient);
        Assert.Equal(140, spectralPassage.CooldownTicks);
        Assert.Equal(
            0.03f,
            ward.Attributes.Single(attribute => attribute.Attribute == AttributeType.MaxHealth).ScalingCoefficient);
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
        int dodgeChance = 0,
        int? partyNumber = null,
        bool canBasicAttack = true,
        BossStaggerDefinition? staggerDefinition = null,
        int staggerParticipantCount = 1) =>
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
            ["Role.Test"],
            canBasicAttack: canBasicAttack,
            partyNumber: partyNumber,
            staggerDefinition: staggerDefinition,
            staggerParticipantCount: staggerParticipantCount);

    private static void AddStandardCondition(
        RuntimeCombatant owner,
        RuntimeCombatant source,
        StandardConditionType condition) =>
        owner.Conditions.Add(
            new RuntimeCondition(
                condition,
                source,
                owner,
                1,
                100,
                source.GetAttribute(AttributeType.Power),
                owner.Conditions.Count + 1,
                $"condition.{condition.ToString().ToLowerInvariant()}"));

    private static RuntimeCombatant CreateOwnedBroodling(
        string id,
        RuntimeCombatant owner,
        int health)
    {
        var broodling = new RuntimeCombatant(
            id,
            id,
            owner.Team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 50,
                [AttributeType.Power] = 0,
                [AttributeType.AttackSpeed] = 0
            },
            [],
            ["Summoned", "Summon.morrowmawBroodling"],
            isSummoned: true,
            summonOwner: owner);
        broodling.SetHealth(health);
        return broodling;
    }

    private static RuntimeCombatant CreateOwnedNiCopy(
        string id,
        RuntimeCombatant owner,
        int health)
    {
        var copy = new RuntimeCombatant(
            id,
            id,
            owner.Team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 0,
                [AttributeType.AttackSpeed] = 0
            },
            [],
            ["Summoned", "Summon.niCopy"],
            isSummoned: true,
            summonOwner: owner,
            canBasicAttack: false);
        copy.SetHealth(health);
        return copy;
    }

    private static RuntimeCombatant CreateOwnedVenomspawn(
        string id,
        RuntimeCombatant owner,
        int health)
    {
        var venomspawn = new RuntimeCombatant(
            id,
            id,
            owner.Team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20,
                [AttributeType.AttackSpeed] = 0
            },
            [],
            ["Summoned", "Summon.venomSpawn"],
            isSummoned: true,
            summonOwner: owner);
        venomspawn.SetHealth(health);
        return venomspawn;
    }

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

    private static AbilitySpec CreateSpiritBloomPassive() =>
        new()
        {
            Id = "ability.creature.forest_spirit.spirit_bloom",
            Kind = AbilitySpecKind.Passive,
            Name = "Spirit Bloom",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnHeal,
                    EveryNthOccurrence = 3,
                    EffectIds = ["effect.forest.spirit.bloom"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.forest.spirit.bloom",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.EventTarget,
                    BaseValue = 30,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        };

    private static AbilityEffectSpec CreateFixedHeal(
        string id,
        int durationTicks = 0,
        int intervalTicks = 0) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.Heal,
            Target = AbilityTargetSelector.Self,
            BaseValue = 10,
            DurationTicks = durationTicks,
            IntervalTicks = intervalTicks,
            CritEligibility = CritEligibility.Disallowed
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

    private static IEnumerable<int> DamageMagnitudes(CombatResult result, string source) =>
        result.EventLog
            .Where(log => log.Source == source && log.EventType == EventType.Damage)
            .Select(log => log.Magnitude);

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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.4f,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static AbilitySpec CreateFixedDamageAbility(string abilityId, string effectId, int damage) =>
        new()
        {
            Id = abilityId,
            Kind = AbilitySpecKind.Active,
            Name = abilityId,
            Effects =
            [
                new()
                {
                    Id = effectId,
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = damage,
                    AttackType = AttackType.None,
                    DamageType = DamageType.None,
                    CritEligibility = CritEligibility.Disallowed
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
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.06f,
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
                    ScalingAttribute = AttributeType.MaxHealth,
                    ScalingCoefficient = 0.01f
                }
            ]
        };

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
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
        var configuredRoot = Environment.GetEnvironmentVariable("LL_TEST_API_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot)
            && File.Exists(Path.Combine(configuredRoot, "Data", "combat", "abilities.json"))
            && File.Exists(Path.Combine(configuredRoot, "Data", "combat", "statuses.json"))
            && File.Exists(Path.Combine(configuredRoot, "Data", "combat", "summons.json")))
        {
            return configuredRoot;
        }

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
