using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences.Definitions;

namespace Domain.Models.Dungeons.Definitions.Boons;

public sealed class DungeonBoonDefinition
{
    public string Id { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DungeonBoonRarity Rarity { get; set; }
    public int Tier { get; set; } = 1;
    public int MaxStacks { get; set; } = 1;
    public int MaxFamilyStacks { get; set; }
    public List<string> EssenceTags { get; set; } = [];
    public List<EssenceAttributeModifier> AttributeModifiers { get; set; } = [];
    public List<EssenceAbilityModifierDefinition> AbilityModifiers { get; set; } = [];
}

public enum DungeonBoonRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Legacy = 5
}
