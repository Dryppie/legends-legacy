namespace Domain.Models.Quests;

public sealed class CharacterQuestProgress
{
    public Guid CharacterId { get; set; }
    public string QuestId { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public QuestStatus Status { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? RewardsGrantedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public ICollection<CharacterQuestObjectiveProgress> Objectives { get; set; } = [];
}
