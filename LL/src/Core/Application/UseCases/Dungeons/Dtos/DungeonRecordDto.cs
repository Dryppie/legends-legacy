namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRecordDto
{
    public bool HasCleared { get; set; }
    public DateTimeOffset? FirstClearedAt { get; set; }
    public DateTimeOffset? LastClearedAt { get; set; }
    public int TotalClears { get; set; }
}
