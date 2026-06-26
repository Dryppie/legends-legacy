namespace Domain.Models.Achievements;

public sealed class AchievementDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public string? PlayerSystemMessageTemplate { get; set; }
    public string? GlobalSystemMessageTemplate { get; set; }
    public AchievementCategory Category { get; set; }
    public AchievementType Type { get; set; }
    public AchievementScope Scope { get; set; }
    public AchievementVisibility Visibility { get; set; }
    public TitleRarity Rarity { get; set; }
    public int Points { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? IconKey { get; set; }
    public AchievementRequirementType RequirementType { get; set; }
    public string? RequirementTarget { get; set; }
    public long RequirementAmount { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
