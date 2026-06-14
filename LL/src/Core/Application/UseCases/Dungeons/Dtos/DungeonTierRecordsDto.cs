namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonTierRecordsDto
{
    public string DungeonDefinitionId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public List<DungeonRecordEntryDto> Records { get; set; } = [];
}
