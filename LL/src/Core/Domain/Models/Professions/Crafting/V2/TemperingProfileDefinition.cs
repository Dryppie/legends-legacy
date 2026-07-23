namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingProfileDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<TemperingStatWeightDefinition> Stats { get; init; } = [];
}
