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
        if (equipment.EquipmentBase.EquipmentType == EquipmentType.Tool ||
            string.IsNullOrWhiteSpace(equipment.BaseRecipeId))
        {
            return null;
        }

        var recipe = _definitions.GetRecipe(equipment.BaseRecipeId);
        var blueprint = string.IsNullOrWhiteSpace(equipment.BlueprintId)
            ? null
            : _definitions.GetBlueprint(equipment.BlueprintId);
        return recipe is null || (!string.IsNullOrWhiteSpace(equipment.BlueprintId) && blueprint is null)
            ? null
            : EquipmentCraftingDesignComposer.Compose(recipe, blueprint).TemperingProfile;
    }
}
