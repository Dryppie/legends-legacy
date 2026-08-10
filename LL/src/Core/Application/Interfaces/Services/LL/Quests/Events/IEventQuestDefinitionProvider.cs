using Application.Interfaces.Services.LL.Quests;

namespace Application.Interfaces.Services.LL.Quests.Events;

public interface IEventQuestDefinitionProvider
{
    IReadOnlyList<EventQuestDefinition> GetAll();
    EventQuestDefinition Get(string eventQuestId, int? version = null);
}

public sealed class EventQuestDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DateTimeOffset ClaimEndsAtUtc { get; set; }
    public long MinimumContribution { get; set; } = 1;
    public int SortOrder { get; set; }
    public List<QuestObjectiveDefinition> Objectives { get; set; } = [];
    public List<QuestRewardDefinition> Rewards { get; set; } = [];
    public List<EventQuestPersonalMilestoneDefinition> PersonalMilestones { get; set; } = [];
}

public sealed class EventQuestPersonalMilestoneDefinition
{
    public string Key { get; set; } = string.Empty;
    public long RequiredContribution { get; set; }
    public List<QuestRewardDefinition> Rewards { get; set; } = [];
}
