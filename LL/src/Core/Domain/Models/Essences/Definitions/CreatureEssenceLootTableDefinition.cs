namespace Domain.Models.Essences.Definitions;

public sealed class CreatureEssenceLootTableDefinition
{
    public string CreatureId { get; set; } = string.Empty;
    public double BaseDropChance { get; set; }
    public string PassiveAbilityId { get; set; } = string.Empty;
    public List<CreatureEssenceVariantDefinition> Variants { get; set; } = [];
}

public sealed class CreatureEssenceVariantDefinition
{
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public string ActiveAbilityId { get; set; } = string.Empty;
    public double Weight { get; set; } = 1;
}
