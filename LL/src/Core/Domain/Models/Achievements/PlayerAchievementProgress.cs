namespace Domain.Models.Achievements;

public sealed class PlayerAchievementProgress
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? CharacterId { get; set; }
    public Guid AchievementDefinitionId { get; set; }
    public AchievementDefinition AchievementDefinition { get; set; } = null!;
    public int? SeasonId { get; set; }
    public long CurrentAmount { get; set; }
    public long RequiredAmount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByCharacterId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
