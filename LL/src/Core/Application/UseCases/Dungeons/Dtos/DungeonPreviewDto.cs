using Application.UseCases.Items.Dtos;
using Domain.Models.Dungeons.Definitions;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int RecommendedPowerScore { get; set; }
    public int MinimumPowerScore { get; set; }
    public string? RequiredPreviousDungeonId { get; set; }
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public DungeonTier DungeonTier { get; set; }
    public List<DungeonPreviewRewardDto> Rewards { get; set; } = [];
}

public sealed class DungeonPreviewRewardDto
{
    public string Id { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
}
