using Domain.Models.AbilityDefinitions;

namespace Domain.Models.Essences.Definitions;

public sealed class EssenceEvolutionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequiredAscensionTier { get; set; }
    public string RequiredCatalystItemId { get; set; } = string.Empty;
    public List<string> AddsTags { get; set; } = [];
    public List<EssenceAttributeBonusDefinition> AttributeModifierChanges { get; set; } = [];
    public List<AbilityModifierDefinition> ActiveAbilityModifiers { get; set; } = [];
    public List<AbilityModifierDefinition> PassiveAbilityModifiers { get; set; } = [];
}
