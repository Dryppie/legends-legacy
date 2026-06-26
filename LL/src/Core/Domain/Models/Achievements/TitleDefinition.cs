namespace Domain.Models.Achievements;

public sealed class TitleDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AchievementCategory Category { get; set; }
    public TitleRarity Rarity { get; set; }
    public TitleScope Scope { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHiddenUntilUnlocked { get; set; }
    public string? SourceAchievementKey { get; set; }
    public int? SeasonNumber { get; set; }
    public string? IconKey { get; set; }
    public int SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
