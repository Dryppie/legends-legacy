using Domain.Models.Bonuses;

namespace Domain.Models.Essences.Definitions;

public sealed class EssenceCodexCollectionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> EssenceDefinitionIds { get; set; } = [];
    public EssenceCodexCollectionBonusDefinition Bonus { get; set; } = new();
}

public sealed class EssenceCodexCollectionBonusDefinition
{
    public BonusKind Kind { get; set; }
    public double Value { get; set; }
    public double ValuePerCollectionAscensionTier { get; set; }
    public string Description { get; set; } = string.Empty;
}
