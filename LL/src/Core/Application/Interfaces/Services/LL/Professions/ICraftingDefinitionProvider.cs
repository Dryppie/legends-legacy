using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Sets;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingDefinitionProvider
{
    IReadOnlyList<MaterialDefinition> GetMaterials();
    IReadOnlyList<CraftingRecipeDefinition> GetRecipes();
    IReadOnlyList<BlueprintDefinition> GetBlueprints();
    IReadOnlyList<EquipmentSetDefinition> GetEquipmentSets();
    IReadOnlyDictionary<string, EquipmentBase> GetEquipmentBases();
    MaterialDefinition? GetStandardMaterial(MaterialFamily family, int tier);
    MaterialDefinition? GetMaterialByItemId(string itemId);
    CraftingRecipeDefinition? GetRecipe(string recipeId);
    BlueprintDefinition? GetBlueprint(string blueprintId);
    BlueprintDefinition? GetBlueprintByItemId(string itemId);
    EquipmentSetDefinition? GetEquipmentSet(string equipmentSetId);
}
