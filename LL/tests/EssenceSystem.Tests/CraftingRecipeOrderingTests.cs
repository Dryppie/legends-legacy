using Application.UseCases.Crafting.Dtos;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingRecipeOrderingTests
{
    [Fact]
    public void OrderGroupsArmorFamiliesWithHeadChestLegsSlotOrder()
    {
        var recipes = new[]
        {
            Armor("medium-legs", "Medium Leggings", "Medium", EquipmentType.Legs),
            Armor("heavy-chest", "Heavy Breastplate", "Heavy", EquipmentType.Chest),
            Armor("medium-head", "Medium Helm", "Medium", EquipmentType.Head),
            Armor("heavy-legs", "Heavy Legplates", "Heavy", EquipmentType.Legs),
            Armor("medium-chest", "Medium Mail", "Medium", EquipmentType.Chest),
            Armor("heavy-head", "Heavy Helm", "Heavy", EquipmentType.Head)
        };

        var orderedIds = CraftingRecipeOrdering.Order(recipes)
            .Select(recipe => recipe.Id)
            .ToArray();

        Assert.Equal(
            [
                "heavy-head",
                "heavy-chest",
                "heavy-legs",
                "medium-head",
                "medium-chest",
                "medium-legs"
            ],
            orderedIds);
    }

    [Fact]
    public void OrderKeepsNonArmorRecipesAlphabeticalWithinCategory()
    {
        var recipes = new[]
        {
            Recipe("wand", "Wand", CraftType.WeaponSmithing, EquipmentType.OneHanded),
            Recipe("dagger", "Dagger", CraftType.WeaponSmithing, EquipmentType.OneHanded),
            Recipe("ring", "Ring", CraftType.JewelryCrafting, EquipmentType.Ring)
        };

        var orderedIds = CraftingRecipeOrdering.Order(recipes)
            .Select(recipe => recipe.Id)
            .ToArray();

        Assert.Equal(["ring", "dagger", "wand"], orderedIds);
    }

    private static CraftingRecipeDto Armor(
        string id,
        string name,
        string family,
        EquipmentType slot) =>
        new()
        {
            Id = id,
            Name = name,
            Category = CraftType.ArmorForging,
            OutputItemType = slot,
            Behavior = new EquipmentBehaviorDefinition { Role = family }
        };

    private static CraftingRecipeDto Recipe(
        string id,
        string name,
        CraftType category,
        EquipmentType outputItemType) =>
        new()
        {
            Id = id,
            Name = name,
            Category = category,
            OutputItemType = outputItemType
        };
}
