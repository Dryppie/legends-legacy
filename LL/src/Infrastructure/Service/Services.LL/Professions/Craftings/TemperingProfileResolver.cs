using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Professions.Craftings;

public sealed class TemperingProfileResolver : ITemperingProfileResolver
{
    private readonly ICraftingDefinitionProvider _definitions;

    public TemperingProfileResolver(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public TemperingProfileDefinition? ResolveFor(EquipmentInstance equipment)
    {
        if (equipment.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return null;

        if (!string.IsNullOrWhiteSpace(equipment.BlueprintId))
        {
            var blueprintProfile = _definitions.GetBlueprint(equipment.BlueprintId)?.TemperingProfile;
            if (blueprintProfile != null)
                return blueprintProfile;
        }

        var recipeProfile = ResolveRecipeProfile(equipment.RecipeId);
        if (recipeProfile != null)
            return recipeProfile;

        return ResolveRecipeProfile(equipment.BaseRecipeId);
    }

    private TemperingProfileDefinition? ResolveRecipeProfile(string? recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
            return null;

        return _definitions.GetRecipe(recipeId)?.TemperingProfile;
    }
}
