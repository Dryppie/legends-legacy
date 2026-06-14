namespace Domain.Models.Essences.Definitions;

public sealed class EssenceAscensionTierDefinition
{
    public int Tier { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public string? RequiredItemId { get; set; }
    public int RequiredItemAmount { get; set; }
}
