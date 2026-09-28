using Application.UseCases.Equipments.Queries.CompareEquipment;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Engine;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class PenetrationRulesTests
{
    [Theory]
    [InlineData(0, 40, 0, 0)]
    [InlineData(55, 40, 0, 0)]
    [InlineData(165, 40, 0, 0)]
    [InlineData(495, 40, 0, .2)]
    [InlineData(495, 100, 0, .2)]
    [InlineData(495, 20, 0, .4)]
    [InlineData(495, -10, 0, .6)]
    [InlineData(495, 40, 50, .08)]
    public void Penetration_subtracts_points_after_corrosion_with_a_floor_and_cap(
        double rating, float penetration, float corrosion, double expected) =>
        Assert.Equal(expected, AttributeRules.Mitigation(rating, penetration, corrosion), 6);

    [Theory]
    [InlineData(18, DamageType.Physical, 10, 30, 60)]
    [InlineData(18, DamageType.Magical, 10, 50, 60)]
    [InlineData(18, DamageType.Physical, 70, 0, 60)]
    [InlineData(17, DamageType.Physical, 60, 0, 51)]
    [InlineData(17, DamageType.Magical, 60, 0, 51)]
    public void Engine_uses_versioned_penetration_and_keeps_general_reduction_separate(
        int version, DamageType damageType, float penetration, float bonus, int expectedDamage)
    {
        var ability = AbilityCompiler.CompileAbility(new AbilitySpec
        {
            Id = "penetration-test", Name = "Penetration test", Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Effects = [new() { Id = "hit", Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget, BaseValue = 100,
                DamageType = damageType, CritEligibility = CritEligibility.Disallowed,
                ArmorPenetrationBonus = bonus }]
        });
        var source = new RuntimeCombatant("source", "source", CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000,
                [AttributeType.ArmorPenetration] = penetration, [AttributeType.MagicPenetration] = penetration },
            [ability], canBasicAttack: false, attributeRulesVersion: version);
        var target = new RuntimeCombatant("target", "target", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000,
                [AttributeType.Armor] = 60, [AttributeType.Resistance] = 60,
                [AttributeType.DamageReduction] = 25 }, [], canBasicAttack: false, attributeRulesVersion: version);
        new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1))
            .Run([source], [target]);
        Assert.Equal(1000 - expectedDamage, target.Health);
    }

    [Theory]
    [InlineData(17, 60)]
    [InlineData(18, 40)]
    public void Metadata_projection_and_equipment_caps_follow_the_rules_version(int version, float cap)
    {
        foreach (var attribute in new[] { AttributeType.ArmorPenetration, AttributeType.MagicPenetration })
        {
            var metadata = AttributeCatalog.GetAll(version).Single(x => x.AttributeType == attribute);
            Assert.Equal(cap, metadata.MaximumValue);
            Assert.Equal(AttributeUnit.PercentagePoints, metadata.Unit);
            Assert.Equal(AttributeCapKind.Fixed, metadata.CapKind);
            Assert.Equal(cap, AttributeCalculator.CalculateProjectedAttributes(
                new Dictionary<AttributeType, float> { [attribute] = 85 }, [], version)[attribute]);
            Assert.Equal(cap, EquipmentStatBudgetCatalog.GetForVersion(attribute, version).PerItemHardCap);
        }
    }

    [Theory]
    [InlineData(17, 60, 10)]
    [InlineData(18, 40, 30)]
    public void Equipment_comparison_reports_versioned_cap_waste(int version, float effective, float waste)
    {
        var character = new Character { Id = Guid.NewGuid(), Level = 30, AttributeRulesVersion = version,
            BaseAttributes = [new() { AttributeType = AttributeType.MaxHealth, Value = 100 },
                new() { AttributeType = AttributeType.ArmorPenetration, Value = 35 }] };
        character.EquipmentSlots = Enum.GetValues<EquipmentSlotType>().Select(slot => new EquipmentSlot
            { EntityId = character.Id, Entity = character, EquipmentSlotType = slot }).ToList();
        var candidate = ProgressionTestEquipment.Create(equipmentType: EquipmentType.Ring,
            stats: new Dictionary<AttributeType, float> { [AttributeType.ArmorPenetration] = 35 });
        Assert.True(EquipmentComparisonProjector.TryProject(character, candidate, null, [], out var result));
        var stat = Assert.Single(result!.Breakdown, x => x.AttributeType == AttributeType.ArmorPenetration);
        Assert.Equal(70, stat.WithSetsAndLoadout);
        Assert.Equal(effective, stat.Effective);
        Assert.Equal(waste, stat.UnusedAtCap);
    }

    [Fact]
    public void Maximum_quality_ranked_gear_respects_penetration_caps_without_losing_budget()
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(),
            "Data", "equipment", "equipment-starters.v1.json"));
        var cappedAttributes = new HashSet<AttributeType>();
        foreach (var definition in catalog.Evaluator.Definitions)
        {
            var archetype = catalog.Evaluator.GetArchetype(definition.ArchetypeId);
            var result = catalog.Evaluator.Evaluate(definition.Id, archetype.MaximumTier,
                EquipmentBalance.MaximumRank, definition.NativeStyleId, ItemQuality.Masterpiece, 1.05);
            Assert.Equal(result.TargetBudget, result.Allocation!.Total, 5);
            foreach (var attribute in new[] { AttributeType.ArmorPenetration, AttributeType.MagicPenetration })
            {
                var points = result.Stats.GetValueOrDefault(attribute);
                Assert.InRange(points, 0, AttributeRules.TypedPenetrationCap);
                if (points == AttributeRules.TypedPenetrationCap) cappedAttributes.Add(attribute);
            }
        }
        Assert.Contains(AttributeType.ArmorPenetration, cappedAttributes);
        Assert.Contains(AttributeType.MagicPenetration, cappedAttributes);
    }

    [Fact]
    public void Current_caps_preserve_raw_buffs_and_historical_frozen_reinforcement()
    {
        var actor = new RuntimeCombatant("source", "source", CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.ArmorPenetration] = 35 }, [], attributeRulesVersion: 18);
        actor.AdjustAttribute(AttributeType.ArmorPenetration, 20);
        Assert.Equal(40, actor.GetAttribute(AttributeType.ArmorPenetration));
        Assert.Equal(55, actor.Attributes[AttributeType.ArmorPenetration]);
        actor.AdjustAttribute(AttributeType.ArmorPenetration, -20);
        Assert.Equal(35, actor.GetAttribute(AttributeType.ArmorPenetration));

        var legacy = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(),
            "Data", "equipment", "equipment-starters.v1.json"), balanceVersion: 1);
        var state = EquipmentState.Award(Guid.NewGuid(), legacy.Evaluator, "plain.wand", 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "test", "test"), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));
        var original = EquipmentData.Create(state, legacy.Evaluator);
        var frozen = new EquipmentData(original.State, original.ItemBaseId, original.DisplayName, original.Rarity,
            original.EquipmentType, original.Behavior, new Dictionary<AttributeType, float>
                { [AttributeType.Power] = 10, [AttributeType.MagicPenetration] = 50 }, original.EquipmentSetId);
        Assert.Equal(52, frozen.ReinforceFrozen(legacy.Evaluator.Balance).Stats[AttributeType.MagicPenetration]);
    }
}
