namespace Domain.Models.Dungeons.Requirements;
public sealed class QuestRequirement : Requirement
{
    public Guid QuestId { get; private set; }
    public string Title { get; private set; } = "";
    public QuestRequirement(Guid questId, string title) { Discriminator = nameof(QuestRequirement); QuestId = questId; Title = title; }
    public override bool IsSatisfiedBy(PlayerContext p) => p.HasCompletedQuest(QuestId);
}
