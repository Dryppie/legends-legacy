namespace Domain.Models.Dungeons.Definitions.Treasures;

public sealed class TreasureRoomDefinition
{
    public Guid Id { get; init; }
    public List<TreasureOptionDefinition> Options { get; init; } = [];
}