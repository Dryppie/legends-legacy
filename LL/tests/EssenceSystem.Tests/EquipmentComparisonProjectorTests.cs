using Application.UseCases.Equipments.Queries.CompareEquipment;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;

namespace EssenceSystem.Tests;

public sealed class EquipmentComparisonProjectorTests
{
    [Fact]
    public void Projection_aggregates_complete_rating_before_applying_diminishing_returns()
    {
        var character = CharacterWithSlots(level: 1);
        Equip(character, EquipmentSlotType.Head, Equipment(EquipmentType.Head, 50));
        Equip(character, EquipmentSlotType.Chest, Equipment(EquipmentType.Chest, 50));
        var candidate = Equipment(EquipmentType.Chest, 100);

        var projected = EquipmentComparisonProjector.TryProject(
            character,
            candidate,
            EquipmentSlotType.Chest,
            [],
            out var comparison);

        Assert.True(projected);
        Assert.NotNull(comparison);
        var rating = Assert.Single(comparison.Ratings, value => value.AttributeType == AttributeType.Armor);
        Assert.Equal(100, rating.Before);
        Assert.Equal(150, rating.After);
        var effective = Assert.Single(comparison.EffectiveAttributes, value => value.AttributeType == AttributeType.Armor);
        Assert.Equal(
            EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(AttributeType.Armor, 100, 1),
            effective.Before,
            precision: 3);
        Assert.Equal(
            EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(AttributeType.Armor, 150, 1),
            effective.After,
            precision: 3);
    }

    [Fact]
    public void Two_handed_projection_replaces_both_hand_items_without_double_counting()
    {
        var character = CharacterWithSlots(level: 1);
        Equip(character, EquipmentSlotType.MainHand, Equipment(EquipmentType.OneHanded, 30));
        Equip(character, EquipmentSlotType.OffHand, Equipment(EquipmentType.OffHand, 40));
        var candidate = Equipment(EquipmentType.TwoHanded, 90);

        Assert.True(EquipmentComparisonProjector.TryProject(
            character,
            candidate,
            EquipmentSlotType.MainHand,
            [],
            out var comparison));

        var rating = Assert.Single(comparison!.Ratings, value => value.AttributeType == AttributeType.Armor);
        Assert.Equal(70, rating.Before);
        Assert.Equal(90, rating.After);
    }

    private static Character CharacterWithSlots(int level)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Level = level,
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 }
            ]
        };
        character.EquipmentSlots = Enum.GetValues<EquipmentSlotType>()
            .Select(slot => new EquipmentSlot
            {
                EntityId = character.Id,
                Entity = character,
                EquipmentSlotType = slot
            })
            .ToList();
        return character;
    }

    private static void Equip(
        Character character,
        EquipmentSlotType slot,
        EquipmentInstance equipment)
    {
        var target = character.EquipmentSlots.Single(value => value.EquipmentSlotType == slot);
        target.EquipmentInstance = equipment;
        target.EquipmentInstanceId = equipment.Id;
    }

    private static EquipmentInstance Equipment(EquipmentType type, float armorRating)
    {
        var id = Guid.NewGuid();
        return new EquipmentInstance
        {
            Id = id,
            ItemBaseId = $"test.{id:N}",
            ItemBase = new EquipmentBase
            {
                Id = $"test.{id:N}",
                Name = "Test equipment",
                EquipmentType = type
            },
            BaseRecipeId = $"recipe.test.{id:N}",
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.Armor, armorRating)
                {
                    Id = Guid.NewGuid(),
                    ItemInstanceId = id
                }
            ]
        };
    }
}
