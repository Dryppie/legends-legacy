namespace Domain.Models.Essences.Definitions;

public sealed class EssenceAscensionTierDefinition
{
    public int Tier { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public string? RequiredCoreItemId { get; set; }
}
