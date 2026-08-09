namespace Application.Interfaces.Services.LL.Quests;

public interface IQuestDefinitionProvider
{
    IReadOnlyList<QuestDefinition> GetAll();
    QuestDefinition Get(string questId, int? version = null);
    bool TryGet(string questId, out QuestDefinition definition);
}

public sealed class QuestDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public QuestChainDefinition? Chain { get; set; }
    public QuestChoiceDefinition? Choice { get; set; }
    public int SortOrder { get; set; }
    public string ObjectiveMode { get; set; } = "Sequential";
    public QuestAvailabilityDefinition Availability { get; set; } = new();
    public List<QuestObjectiveDefinition> Objectives { get; set; } = [];
    public List<QuestRewardDefinition> Rewards { get; set; } = [];
}

public sealed class QuestChoiceDefinition
{
    public string SelectionTitle { get; set; } = string.Empty;
    public string SelectionSummary { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
    public List<QuestChoiceOptionDefinition> Options { get; set; } = [];
}

public sealed class QuestChoiceOptionDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid CreatureId { get; set; }
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public string RewardItemBaseId { get; set; } = string.Empty;
    public string EncounterKey { get; set; } = string.Empty;

    public string CreatureName { get; set; } = string.Empty;
}

public sealed class QuestChainDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Step { get; set; }
    public int TotalSteps { get; set; }
}

public sealed class QuestAvailabilityDefinition
{
    public int MinimumLevel { get; set; } = 1;
    public List<string> CompletedQuestIds { get; set; } = [];
}

public sealed class QuestObjectiveDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long RequiredAmount { get; set; } = 1;
    public QuestObjectiveFilterDefinition Filters { get; set; } = new();
    public QuestPresentationDefinition Presentation { get; set; } = new();
}

public sealed class QuestObjectiveFilterDefinition
{
    public string? AreaId { get; set; }
    public bool? RequiresVictory { get; set; }
    public string? EssenceDefinitionId { get; set; }
    public string? EssenceDefinitionFromChoiceQuestId { get; set; }
    public int? Tier { get; set; }
    public bool MustBeCrafted { get; set; }
    public bool ToolSlotOnly { get; set; }
    public List<string> ItemBaseIds { get; set; } = [];
}

public sealed class QuestPresentationDefinition
{
    public string ActionLabel { get; set; } = string.Empty;
    public string DestinationRoute { get; set; } = string.Empty;
    public string? GuidePageId { get; set; }
    public string? TourPageId { get; set; }
}

public sealed class QuestRewardDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ItemBaseId { get; set; }
    public int Quantity { get; set; } = 1;
}
