using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class PowerRatingCoreTests
{
    [Fact]
    public void Build_fingerprint_tracks_combat_inputs_but_ignores_currency()
    {
        var character = CreateCharacter();
        var original = PowerBuildSnapshotFactory.CreateFingerprint(character);

        character.Cinders += 10_000;
        character.Soulstones += 500;
        var afterCurrency = PowerBuildSnapshotFactory.CreateFingerprint(character);

        character.BaseAttributes.Single(x => x.AttributeType == AttributeType.Power).Value += 1;
        var afterPower = PowerBuildSnapshotFactory.CreateFingerprint(character);

        Assert.Equal(original, afterCurrency);
        Assert.NotEqual(original, afterPower);
    }

    [Fact]
    public void Build_fingerprint_is_deterministic_when_attribute_order_changes()
    {
        var first = CreateCharacter();
        var second = CreateCharacter();
        second.BaseAttributes = second.BaseAttributes.Reverse().ToList();

        Assert.Equal(
            PowerBuildSnapshotFactory.CreateFingerprint(first),
            PowerBuildSnapshotFactory.CreateFingerprint(second));
    }

    [Theory]
    [InlineData(0.00, DungeonReadinessBand.VeryUnlikely)]
    [InlineData(0.15, DungeonReadinessBand.Risky)]
    [InlineData(0.40, DungeonReadinessBand.Uncertain)]
    [InlineData(0.60, DungeonReadinessBand.Favored)]
    [InlineData(0.80, DungeonReadinessBand.Comfortable)]
    public void Readiness_bands_use_documented_probability_thresholds(
        double probability,
        DungeonReadinessBand expected)
    {
        Assert.Equal(expected, DungeonReadinessService.GetBand((decimal)probability));
    }

    [Fact]
    public void Wilson_interval_expresses_uncertainty_in_small_samples()
    {
        var interval = DungeonReadinessService.WilsonInterval(3, 8);

        Assert.True(interval.Lower < 0.375m);
        Assert.True(interval.Upper > 0.375m);
        Assert.InRange(interval.Lower, 0m, 1m);
        Assert.InRange(interval.Upper, 0m, 1m);
    }

    [Fact]
    public void Algorithm_and_seed_versions_are_explicit()
    {
        Assert.True(PowerRatingAlgorithm.Version > 0);
        Assert.True(PowerRatingAlgorithm.BenchmarkDefinitionVersion > 0);
        Assert.True(PowerRatingAlgorithm.RatingSeedSetVersion > 0);
        Assert.True(PowerRatingAlgorithm.DungeonSeedSetVersion > 0);
    }

    [Theory]
    [InlineData(DungeonTier.Normal, 1)]
    [InlineData(DungeonTier.Heroic, 2)]
    [InlineData(DungeonTier.Mythic, 3)]
    public void Api_dungeon_tiers_map_to_one_based_definition_tiers(
        DungeonTier apiTier,
        int definitionTier)
    {
        Assert.Equal(definitionTier, apiTier.ToDefinitionTier());
        Assert.Equal(apiTier, definitionTier.ToDungeonTier());
    }

    [Fact]
    public async Task Physical_and_magical_durability_use_equivalent_controlled_pressure()
    {
        var executor = new CapturingCombatExecutor();
        var runner = new PowerAnalysisSimulationRunner(
            executor,
            null!,
            null!,
            null!,
            null!,
            null!);
        var party = new[] { new CombatEntity(CreateCharacter()) };

        await runner.MeetsBenchmarkAsync(
            party,
            PowerBenchmarkScenario.PhysicalDurability,
            3,
            17,
            CancellationToken.None);
        await runner.MeetsBenchmarkAsync(
            party,
            PowerBenchmarkScenario.MagicalDurability,
            3,
            17,
            CancellationToken.None);

        var physical = executor.Captures[0];
        var magical = executor.Captures[1];
        Assert.True(physical.Options.StartActiveAbilitiesOnCooldown);
        Assert.True(magical.Options.StartActiveAbilitiesOnCooldown);
        Assert.Equal(int.MaxValue, physical.Options.BasicAttackIntervalTicks);
        Assert.Equal(int.MaxValue, magical.Options.BasicAttackIntervalTicks);

        var physicalAbility = GetHostileAbility(physical);
        var magicalAbility = GetHostileAbility(magical);
        Assert.Equal(physicalAbility.CooldownTicks, magicalAbility.CooldownTicks);
        Assert.Equal(
            physicalAbility.Effects.Single().BaseValue,
            magicalAbility.Effects.Single().BaseValue);
        Assert.Equal(
            physicalAbility.Effects.Single().ScalingCoefficient,
            magicalAbility.Effects.Single().ScalingCoefficient);
        Assert.Equal(DamageType.Physical, physicalAbility.Effects.Single().DamageType);
        Assert.Equal(DamageType.Magical, magicalAbility.Effects.Single().DamageType);
    }

    [Fact]
    public async Task Overall_benchmark_scales_mixed_incoming_pressure_with_intensity()
    {
        var executor = new CapturingCombatExecutor();
        var runner = new PowerAnalysisSimulationRunner(
            executor,
            null!,
            null!,
            null!,
            null!,
            null!);
        var party = new[] { new CombatEntity(CreateCharacter()) };

        await runner.MeetsBenchmarkAsync(
            party,
            PowerBenchmarkScenario.Overall,
            3,
            17,
            CancellationToken.None);
        await runner.MeetsBenchmarkAsync(
            party,
            PowerBenchmarkScenario.Overall,
            9,
            17,
            CancellationToken.None);

        var lowerPressure = executor.Captures[0].Runtime.HostileParticipants;
        var higherPressure = executor.Captures[1].Runtime.HostileParticipants;
        Assert.Equal(2, lowerPressure.Count);
        Assert.Equal(2, higherPressure.Count);
        Assert.All(higherPressure, hostile => Assert.True(
            hostile.Combatant.GetAttributeValue(AttributeType.Power) >
            lowerPressure.Single(lower => lower.Slot.SlotId == hostile.Slot.SlotId)
                .Combatant.GetAttributeValue(AttributeType.Power)));
        Assert.Single(higherPressure, hostile => hostile.Combatant.NativeAbilityIds.Count > 0);
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    public async Task Overall_benchmark_requires_a_healthy_victory(
        int remainingHealth,
        bool expected)
    {
        var runner = new PowerAnalysisSimulationRunner(
            new FixedHealthCombatExecutor(remainingHealth),
            null!,
            null!,
            null!,
            null!,
            null!);

        var result = await runner.MeetsBenchmarkAsync(
            [new CombatEntity(CreateCharacter())],
            PowerBenchmarkScenario.Overall,
            3,
            17,
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AbilityTargetSelector.CurrentTarget, false)]
    [InlineData(AbilityTargetSelector.AllEnemies, true)]
    public async Task Area_damage_benchmark_only_credits_attacks_that_reach_secondary_targets(
        AbilityTargetSelector target,
        bool expected)
    {
        const string abilityId = "power-test.area-damage";
        var ability = CreateTestDamageAbility(abilityId, target);
        var runner = new PowerAnalysisSimulationRunner(
            new CombatEngineExecutor(new FixedAbilityCatalogProvider(ability)),
            null!,
            null!,
            null!,
            null!,
            null!);
        var character = CreateCharacter();
        character.BaseAttributes.Single(x => x.AttributeType == AttributeType.MaxHealth).Value = 10_000;
        var combatant = new CombatEntity(character)
        {
            HasEquippedEssenceSnapshot = true,
            NativeAbilityIds = [abilityId]
        };
        AttributeCalculator.CalculateBaseCombatAttributes(combatant);

        var result = await runner.MeetsBenchmarkAsync(
            [combatant],
            PowerBenchmarkScenario.MultiTarget,
            1,
            17,
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task New_character_has_balanced_and_bounded_durability_scores()
    {
        var runner = new PowerAnalysisSimulationRunner(
            new CombatEngineExecutor(new EmptyAbilityCatalogProvider()),
            null!,
            null!,
            null!,
            null!,
            null!);
        var combatant = new CombatEntity(CreateNewAccountCharacter());
        AttributeCalculator.CalculateBaseCombatAttributes(combatant);

        var physical = await FindHighestPassingIntensity(
            runner,
            combatant,
            PowerBenchmarkScenario.PhysicalDurability);
        var magical = await FindHighestPassingIntensity(
            runner,
            combatant,
            PowerBenchmarkScenario.MagicalDurability);

        Assert.Equal(physical, magical);
        Assert.InRange(
            physical * PowerAnalysisSimulationRunner.DisplayPowerPerIntensity,
            10,
            60);
    }

    private static async Task<int> FindHighestPassingIntensity(
        PowerAnalysisSimulationRunner runner,
        CombatEntity combatant,
        PowerBenchmarkScenario scenario)
    {
        var highest = 0;
        for (var intensity = 1; intensity <= 10; intensity++)
        {
            if (!await runner.MeetsBenchmarkAsync(
                    [combatant],
                    scenario,
                    intensity,
                    17,
                    CancellationToken.None))
                break;
            highest = intensity;
        }

        return highest;
    }

    private static Domain.Models.Combat.Abilities.AbilitySpec GetHostileAbility(
        CapturedSimulation capture)
    {
        var abilityId = capture.Runtime.HostileParticipants
            .Single()
            .Combatant.NativeAbilityIds.Single();
        return capture.Options.SupplementalAbilities!.Single(x => x.Id == abilityId);
    }

    private static Character CreateCharacter() => new()
    {
        Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        Name = "Power Fixture",
        Level = 12,
        BaseAttributes =
        [
            new EntityAttribute { AttributeType = AttributeType.Power, Value = 25 },
            new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 250 },
            new EntityAttribute { AttributeType = AttributeType.Resistance, Value = 15 }
        ]
    };

    private static Character CreateNewAccountCharacter() => new()
    {
        Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
        Name = "New Account Fixture",
        Level = 1,
        BaseAttributes =
        [
            new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.Fortitude, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.Precision, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.Spirit, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 },
            new EntityAttribute { AttributeType = AttributeType.HealthRegeneration, Value = 2 }
        ]
    };

    private static AbilitySpec CreateTestDamageAbility(
        string id,
        AbilityTargetSelector target) => new()
    {
        Id = id,
        Kind = AbilitySpecKind.Active,
        Name = id,
        Description = "Power benchmark test ability.",
        CooldownTicks = 30,
        Triggers =
        [
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnAbilityUsed,
                EffectIds = [$"{id}.effect"]
            }
        ],
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = $"{id}.effect",
                Operation = AbilityEffectOperation.Damage,
                Target = target,
                BaseValue = 1_000,
                DamageType = DamageType.Magical,
                AttackType = AttackType.Ranged
            }
        ]
    };

    private sealed class CapturingCombatExecutor : ICombatEngineExecutor
    {
        public List<CapturedSimulation> Captures { get; } = [];

        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatResult> ExecuteSimulationAsync(
            CombatEncounterRuntime runtime,
            CombatSimulationOptions options,
            CancellationToken cancellationToken)
        {
            Captures.Add(new CapturedSimulation(runtime, options));
            return Task.FromResult(new CombatResult
            {
                Outcome = BattleOutcome.Draw,
                Duration = options.MaxTicks
            });
        }
    }

    private sealed class FixedHealthCombatExecutor(int remainingHealth) : ICombatEngineExecutor
    {
        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatResult> ExecuteSimulationAsync(
            CombatEncounterRuntime runtime,
            CombatSimulationOptions options,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CombatResult
            {
                Outcome = BattleOutcome.Victory,
                PlayerTeam =
                [
                    new SimpleCombatEntity
                    {
                        Id = "fixture",
                        Name = "Fixture",
                        MaxHealth = 100,
                        Health = remainingHealth
                    }
                ]
            });
    }

    private sealed record CapturedSimulation(
        CombatEncounterRuntime Runtime,
        CombatSimulationOptions Options);

    private sealed class EmptyAbilityCatalogProvider : IAbilityCatalogProvider
    {
        private static readonly AbilityCatalog Catalog = new(
            [],
            [],
            [],
            new Dictionary<string, string>());

        public AbilityCatalog GetCatalog() => Catalog;
    }

    private sealed class FixedAbilityCatalogProvider(AbilitySpec ability) : IAbilityCatalogProvider
    {
        private readonly AbilityCatalog _catalog = new(
            [ability],
            [],
            [],
            new Dictionary<string, string>());

        public AbilityCatalog GetCatalog() => _catalog;
    }
}
