namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityConditionDefinition
{
    public string Type { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public string? Status { get; set; }
    public double? Value { get; set; }
}
