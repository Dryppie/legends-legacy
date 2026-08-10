namespace Domain.Models.Quests.Events;

public sealed class EventQuestInstance
{
    public string EventQuestId { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public EventQuestStatus Status { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DateTimeOffset ClaimEndsAtUtc { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public ICollection<EventQuestObjectiveProgress> Objectives { get; set; } = [];
    public ICollection<EventQuestCharacterContribution> Contributions { get; set; } = [];
    public ICollection<EventQuestRewardClaim> RewardClaims { get; set; } = [];
    public ICollection<EventQuestMilestoneClaim> MilestoneClaims { get; set; } = [];
}
