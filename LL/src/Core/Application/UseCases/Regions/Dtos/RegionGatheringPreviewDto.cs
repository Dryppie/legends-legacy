namespace Application.UseCases.Regions.Dtos;

public sealed class RegionGatheringPreviewDto
{
    public IReadOnlyList<AreaGatheringPreviewDto> Areas { get; init; } = [];
}

public sealed class AreaGatheringPreviewDto
{
    public string Id { get; init; } = string.Empty;
    public IReadOnlyList<AreaGatheringNodePreviewDto> GatheringNodes { get; init; } = [];
}

public sealed class AreaGatheringNodePreviewDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int? LevelRequirement { get; init; }
    public float ProcChance { get; init; }
    public double YieldBonusPercent { get; init; }
    public int? MinQuantity { get; init; }
    public int? MaxQuantity { get; init; }
}
