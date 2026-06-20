using Domain.Models.Combat.Abilities;

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
    public List<EssenceAbilityModifierDefinition> ActiveAbilityModifiers { get; set; } = [];
    public List<EssenceAbilityModifierDefinition> PassiveAbilityModifiers { get; set; } = [];
}

public sealed class EssenceAbilityModifierDefinition
{
    public string Target { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Condition { get; set; }
    public AbilityEffectSpec? Effect { get; set; }
}
