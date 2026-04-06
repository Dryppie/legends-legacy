namespace Domain.Models.Dungeons.Definitions.Encounters;

public sealed class EncounterDefinition
{
    public string CreatureId { get; init; } = string.Empty;
    public EncounterKind Kind { get; init; }
}
