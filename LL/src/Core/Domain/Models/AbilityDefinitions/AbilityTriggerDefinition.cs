namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityTriggerDefinition
{
    public string Type { get; set; } = string.Empty;
    public double? InternalCooldownSeconds { get; set; }
}
