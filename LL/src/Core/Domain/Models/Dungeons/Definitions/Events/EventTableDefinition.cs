namespace Domain.Models.Dungeons.Definitions.Events;

public sealed class EventTableDefinition
{
    public IReadOnlyList<EventOutcomeWeight> Outcomes { get; init; } =
    [
        new EventOutcomeWeight(EventOutcomeType.ExtraCombat, 40),
        new EventOutcomeWeight(EventOutcomeType.TreasureRoom, 20),
        new EventOutcomeWeight(EventOutcomeType.Shrine, 20),
        new EventOutcomeWeight(EventOutcomeType.Trap, 20),
    ];
}