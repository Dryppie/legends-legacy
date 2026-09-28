using Domain.Models.Attributes;
using Domain.Components.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Sets;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentSetBonusResolverTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Restored_sets_keep_original_thresholds_procs_and_stat_effects_with_current_attributes(int release)
    {
        var path = Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json");
        var original = JsonStarterEquipmentCatalog.Load(path, 1);
        var current = JsonStarterEquipmentCatalog.Load(path, release);
        var baseAttributes = new Dictionary<AttributeType, float>
        {
            [AttributeType.Power] = 100, [AttributeType.MaxHealth] = 1000,
            [AttributeType.HealthRegeneration] = 20,
            [AttributeType.ArmorRating] = 100, [AttributeType.ResistanceRating] = 200
        };
        foreach (var legacySet in original.EquipmentSets)
        {
            var restored = current.GetEquipmentSet(legacySet.Id + ".v2")!;
            Assert.NotNull(restored);
            Assert.Equal(legacySet.Name, restored.Name);
            Assert.Equal(legacySet.Description, restored.Description);
            Assert.Equal(legacySet.Bonuses.Select(b => (b.Id, b.RequiredEquippedItems, b.Enabled)),
                restored.Bonuses.Select(b => (b.Id, b.RequiredEquippedItems, b.Enabled)));
            foreach (var legacyBonus in legacySet.Bonuses)
            {
                var bonus = Assert.Single(restored.Bonuses, b => b.Id == legacyBonus.Id);
                Assert.Equal(legacyBonus.GrantedAbilityIds, bonus.GrantedAbilityIds);
                Assert.All(bonus.AttributeModifiers, modifier => Assert.True(
                    AttributeRules.IsOrdinaryEquipmentAttribute(modifier.AttributeType)));
                Assert.DoesNotContain("Grants ", bonus.Description);
                Assert.DoesNotContain("Cooldown Reduction", bonus.Description);
                Assert.DoesNotContain("Healing Power", bonus.Description);
                Assert.DoesNotContain("Status Resistance", bonus.Description);
                Assert.DoesNotContain("Crowd Control Resistance", bonus.Description);
                if (legacyBonus.AttributeModifiers.Count == 0)
                    Assert.Equal(legacyBonus.Description, bonus.Description);
            }

            foreach (var count in new[] { 2, 4, 6 })
            {
                var legacyEquipment = Enumerable.Range(0, count).Select(_ => CreateItem(legacySet.Id));
                var restoredEquipment = Enumerable.Range(0, count).Select(_ => CreateCurrentItem(restored.Id, release, true));
                // The compatibility projection is an independent reference for
                // translating retired attributes, including CDR and merged resistances.
                var expected = AttributeCalculator.CalculateProjectedAttributes(baseAttributes,
                    EquipmentSetBonusResolver.ResolveAttributeModifiers(legacyEquipment, [legacySet]), 18);
                // Spirit's Restoration and haste amounts were explicitly revised after the restoration.
                if (legacySet.Id == "set_spirit")
                {
                    expected[AttributeType.Restoration] = 25;
                    if (count >= 4) expected[AttributeType.AbilityHaste] = 5;
                }
                var actual = AttributeCalculator.CalculateProjectedAttributes(baseAttributes,
                    EquipmentSetBonusResolver.ResolveAttributeModifiers(restoredEquipment, [restored]), 18);
                Assert.Equal(expected.Keys.Order(), actual.Keys.Order());
                foreach (var (attribute, value) in expected)
                    Assert.Equal(value, actual[attribute], 4);
            }
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Authored_set_stats_are_fixed_across_item_strength_and_piece_counts(int release)
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(),
            "Data", "equipment", "equipment-starters.v1.json"), release);
        foreach (var definition in catalog.EquipmentSets.Where(set => set.UsesReservedIdentity))
        {
            Assert.All(definition.Bonuses, bonus => Assert.DoesNotContain("Scales with", bonus.Description));
            foreach (var pieceCount in new[] { 1, 2, 3, 4, 6 })
            {
                var expected = definition.Bonuses.Where(bonus => bonus.Enabled && bonus.RequiredEquippedItems <= pieceCount)
                    .OrderBy(bonus => bonus.RequiredEquippedItems).ThenBy(bonus => bonus.Id, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(bonus => bonus.AttributeModifiers.Select(modifier =>
                        (bonus.Id, modifier.AttributeType, modifier.Amount, modifier.ModifierType))).ToArray();
                foreach (var upgraded in new[] { false, true })
                {
                    var equipment = Enumerable.Range(0, pieceCount).Select(_ =>
                        CreateCurrentItem(definition.Id, release, upgraded)).ToArray();
                    var actual = EquipmentSetBonusResolver.ResolveAttributeModifiers(equipment, [definition])
                        .Cast<EquipmentSetAttributeModifier>()
                        .Select(modifier => (modifier.BonusId, modifier.AttributeType, modifier.Amount, modifier.ModifierType));
                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Theory]
    [InlineData("set_phoenix.v2", 8f)]
    [InlineData("set_spirit.v2", 25f)]
    public void Two_piece_restoration_is_identical_for_mixed_strength_and_two_handed_equipment(
        string setId, float expected)
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(),
            "Data", "equipment", "equipment-starters.v1.json"), 4);
        var definition = catalog.GetEquipmentSet(setId)!;
        var weak = CreateCurrentItem(definition.Id, 4, false);
        var strong = CreateCurrentItem(definition.Id, 4, true);
        var twoHanded = CreateCurrentItem(definition.Id, 4, true, EquipmentType.TwoHanded);
        foreach (var equipment in new[] { new[] { weak, strong }, new[] { twoHanded, twoHanded } })
        {
            var modifier = Assert.Single(EquipmentSetBonusResolver.ResolveAttributeModifiers(equipment, [definition]));
            Assert.Equal(AttributeType.Restoration, modifier.AttributeType);
            Assert.Equal(expected, modifier.Amount);
        }
    }

    [Fact]
    public void ResolverCountsDistinctInstancesAndActivatesCumulativeThresholdsOnce()
    {
        var definition = CreateDefinition();
        var first = CreateItem("set.test");
        var equipment = new[]
        {
            first,
            first,
            CreateItem("SET.TEST"),
            CreateItem("set.test"),
            CreateItem("set.test")
        };

        var state = Assert.Single(EquipmentSetBonusResolver.Resolve(equipment, [definition]));

        Assert.Equal(4, state.EquippedCount);
        Assert.Equal(["two", "four"], state.ActiveBonuses.Select(active => active.Bonus.Id));

        var modifiers = EquipmentSetBonusResolver.ResolveAttributeModifiers(equipment, [definition]);
        Assert.Collection(
            modifiers,
            modifier =>
            {
                Assert.Equal(AttributeType.CritChance, modifier.AttributeType);
                Assert.Equal(5, modifier.Amount);
            },
            modifier =>
            {
                Assert.Equal(AttributeType.Power, modifier.AttributeType);
                Assert.Equal(10, modifier.Amount);
                Assert.Equal(ModifierType.Multiplicative, modifier.ModifierType);
            });
        Assert.Equal(
            ["ability.set.test.two", "ability.set.test.four"],
            EquipmentSetBonusResolver.ResolveGrantedAbilityIds(equipment, [definition]));
    }

    [Fact]
    public void ResolverFailsClosedForUnknownSetsAndDoesNotActivateUnreachedBonuses()
    {
        var equipment = new[]
        {
            CreateItem("set.unknown"),
            CreateItem("set.test")
        };

        var state = Assert.Single(EquipmentSetBonusResolver.Resolve(equipment, [CreateDefinition()]));

        Assert.Equal("set.test", state.Definition.Id);
        Assert.Equal(1, state.EquippedCount);
        Assert.Empty(state.ActiveBonuses);
        Assert.Empty(EquipmentSetBonusResolver.ResolveAttributeModifiers(equipment, [CreateDefinition()]));
        Assert.Empty(EquipmentSetBonusResolver.ResolveGrantedAbilityIds(equipment, [CreateDefinition()]));
    }

    [Fact]
    public void TwoHandedEquipmentCountsAsTwoSetItemsButDuplicateSlotReferencesCountOnce()
    {
        var twoHanded = CreateItem("set.test", EquipmentType.TwoHanded);

        var state = Assert.Single(EquipmentSetBonusResolver.Resolve(
            [twoHanded, twoHanded],
            [CreateDefinition()]));

        Assert.Equal(2, state.EquippedCount);
        Assert.Equal(["two"], state.ActiveBonuses.Select(active => active.Bonus.Id));
        Assert.Equal([twoHanded.Id], state.EquippedItemInstanceIds);
    }

    private static EquipmentSetDefinition CreateDefinition() => new()
    {
        Id = "set.test",
        Name = "Test",
        Bonuses =
        [
            new EquipmentSetBonusDefinition
            {
                Id = "two",
                RequiredEquippedItems = 2,
                Description = "Two items.",
                AttributeModifiers =
                [
                    new EquipmentSetAttributeModifierDefinition
                    {
                        AttributeType = AttributeType.CritChance,
                        Amount = 5
                    }
                ],
                GrantedAbilityIds = ["ability.set.test.two"]
            },
            new EquipmentSetBonusDefinition
            {
                Id = "four",
                RequiredEquippedItems = 4,
                Description = "Four items.",
                AttributeModifiers =
                [
                    new EquipmentSetAttributeModifierDefinition
                    {
                        AttributeType = AttributeType.Power,
                        Amount = 10,
                        ModifierType = ModifierType.Multiplicative
                    }
                ],
                GrantedAbilityIds = ["ability.set.test.four"]
            }
        ]
    };

    private static EquipmentInstance CreateItem(
        string setId,
        EquipmentType equipmentType = EquipmentType.Chest) =>
        ProgressionTestEquipment.Create(setId, equipmentType);

    private static EquipmentInstance CreateCurrentItem(string setId, int release, bool upgraded,
        EquipmentType equipmentType = EquipmentType.Chest)
    {
        var item = ProgressionTestEquipment.Create(setId, equipmentType);
        var original = item.ProgressionData!;
        var reservation = upgraded ? 900d : 5d;
        var data = new EquipmentData(original.State with
        {
            BalanceVersion = release, Tier = upgraded ? 10 : 1, Rank = upgraded ? 5 : 0,
            Quality = upgraded ? ItemQuality.Masterpiece : ItemQuality.Crude,
            AttributeRollMultiplier = upgraded ? 1.05 : .95
        }, original.ItemBaseId, original.DisplayName,
            upgraded ? EquipmentRarity.Legendary : EquipmentRarity.Common,
            original.EquipmentType, original.Behavior, original.Stats, setId,
            allocation: new EquipmentBudgetBreakdown(18, reservation * 7, reservation * 3,
                reservation / 2, reservation, "default"));
        item.ApplyProgressionData(data);
        return item;
    }
}
