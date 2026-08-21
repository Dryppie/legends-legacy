using Application.UseCases.Crafting.Dtos;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Professions.Craftings;

public static class CraftingRecipeOrdering
{
    private static readonly IReadOnlyDictionary<string, int> ArmorFamilyOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Heavy"] = 0,
            ["Medium"] = 1,
            ["Light"] = 2,
            ["Cloth"] = 3
        };

    public static IOrderedEnumerable<CraftingRecipeDto> Order(
        IEnumerable<CraftingRecipeDto> recipes) =>
        recipes
            .OrderBy(recipe => recipe.Category)
            .ThenBy(GetArmorFamilyOrder)
            .ThenBy(GetArmorFamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(GetArmorSlotOrder)
            .ThenBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase);

    private static int GetArmorFamilyOrder(CraftingRecipeDto recipe) =>
        recipe.Category == CraftType.ArmorForging
            ? ArmorFamilyOrder.GetValueOrDefault(recipe.Behavior.Role, int.MaxValue)
            : 0;

    private static string GetArmorFamilyName(CraftingRecipeDto recipe) =>
        recipe.Category == CraftType.ArmorForging &&
        !ArmorFamilyOrder.ContainsKey(recipe.Behavior.Role)
            ? recipe.Behavior.Role
            : string.Empty;

    private static int GetArmorSlotOrder(CraftingRecipeDto recipe) =>
        recipe.Category == CraftType.ArmorForging
            ? recipe.OutputItemType switch
            {
                EquipmentType.Head => 0,
                EquipmentType.Chest => 1,
                EquipmentType.Legs => 2,
                _ => int.MaxValue
            }
            : 0;
}
