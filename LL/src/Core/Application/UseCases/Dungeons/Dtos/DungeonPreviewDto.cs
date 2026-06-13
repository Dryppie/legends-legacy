using Application.UseCases.Items.Dtos;
using Domain.Models.Dungeons.Definitions;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public string FamilyTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int RecommendedCombatRating { get; set; }
    public int MinimumCombatRating { get; set; }
    public int CurrentCombatRating { get; set; }
    public bool CanEnter { get; set; }
    public List<string> MissingRequirements { get; set; } = [];
    public string? RequiredPreviousDungeonId { get; set; }
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public DungeonTier DungeonTier { get; set; }
    public DungeonRecordDto Record { get; set; } = new();
    public List<DungeonPreviewRewardDto> Rewards { get; set; } = [];
}

public sealed class DungeonPreviewRewardDto
{
    public string Id { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
