namespace Domain.Models.Achievements;

public sealed class PlayerTitleUnlock
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? CharacterId { get; set; }
    public Guid TitleDefinitionId { get; set; }
    public TitleDefinition TitleDefinition { get; set; } = null!;
    public DateTimeOffset UnlockedAt { get; set; }
    public Guid? UnlockedByAchievementDefinitionId { get; set; }
    public int? SeasonId { get; set; }
    public string? MetadataJson { get; set; }
}
