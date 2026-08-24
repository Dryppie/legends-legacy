using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Sets;

namespace EssenceSystem.Tests;

public sealed class EquipmentSetBonusResolverTests
{
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

    private static EquipmentInstance CreateItem(string setId) => new()
    {
        Id = Guid.NewGuid(),
        EquipmentSetId = setId
    };
}
