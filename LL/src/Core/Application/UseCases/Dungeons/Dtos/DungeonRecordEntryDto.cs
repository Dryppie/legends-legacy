namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRecordEntryDto
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public DateTimeOffset FirstClearedAt { get; set; }
    public DateTimeOffset LastClearedAt { get; set; }
    public int TotalClears { get; set; }
}
