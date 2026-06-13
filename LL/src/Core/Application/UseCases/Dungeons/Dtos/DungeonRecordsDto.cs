namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRecordsDto
{
    public string FamilyId { get; set; } = string.Empty;
    public string FamilyTitle { get; set; } = string.Empty;
    public List<DungeonTierRecordsDto> Tiers { get; set; } = [];
}
