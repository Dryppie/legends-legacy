using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;

namespace Application.Interfaces.Services.LL.Quests;

public interface IQuestService
{
    Task<QuestJournal> GetJournalAsync(Guid characterId, CancellationToken cancellationToken);
    Task<QuestJournal> AcknowledgeWelcomeAsync(Guid characterId, CancellationToken cancellationToken);
    Task<QuestJournal> SelectChoiceAsync(
        Guid characterId,
        string questId,
        string optionKey,
        CancellationToken cancellationToken);
    Task<QuestJournal> PinAsync(Guid characterId, string? questId, CancellationToken cancellationToken);
}

public interface IQuestProgressionService
{
    Task<QuestProgressionResult> ProcessAsync(
        Guid characterId,
        QuestTrigger trigger,
        Guid? outboxMessageId,
        string eventType,
        CancellationToken cancellationToken);
}

public sealed record QuestJournal(
    IReadOnlyList<QuestState> Quests,
    string? PinnedQuestId);

public sealed record QuestState(
    string QuestId,
    int Version,
    string Title,
    string Summary,
    string Category,
    string ObjectiveMode,
    QuestChain? Chain,
    QuestChoice? Choice,
    int SortOrder,
    QuestStatus Status,
    bool IsPinned,
    bool RequiresWelcome,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<QuestObjectiveState> Objectives,
    IReadOnlyList<QuestRewardState> Rewards);

public sealed record QuestChoice(
    string SelectionTitle,
    string SelectionSummary,
    string ConfirmationText,
    string? SelectedOptionKey,
    IReadOnlyList<QuestChoiceOption> Options);

public sealed record QuestChoiceOption(
    string Key,
    string Title,
    string Summary,
    Guid CreatureId,
    string CreatureName,
    string EssenceDefinitionId,
    string RewardItemBaseId,
    string EncounterKey,
    ItemBase? RewardItemBase);

public sealed record QuestChain(
    string Id,
    string Title,
    int Step,
    int TotalSteps);

public sealed record QuestObjectiveState(
    string Key,
    string Description,
    string Type,
    long CurrentAmount,
    long RequiredAmount,
    bool IsCompleted,
    QuestPresentation Presentation);

public sealed record QuestRewardState(
    string Key,
    string Type,
    string? ItemBaseId,
    int Quantity,
    ItemBase? ItemBase);

public sealed record QuestPresentation(
    string ActionLabel,
    string DestinationRoute,
    string? GuidePageId,
    string? TourPageId);

public sealed record QuestProgressionResult(
    QuestJournal Journal,
    IReadOnlyList<string> CompletedQuestIds,
    IReadOnlyList<InventoryItem> Loot);

public sealed record QuestTrigger(
    string Type,
    string? AreaId = null,
    bool? WonEncounter = null,
    string? EssenceDefinitionId = null,
    IReadOnlyCollection<string>? CraftedItemBaseIds = null,
    IReadOnlyCollection<int>? CraftedItemTiers = null,
    int? CharacterLevel = null,
    int ActionCount = 1,
    string? EquippedGatheringType = null)
{
    public static QuestTrigger CombatCompleted(
        string areaId,
        bool wonEncounter,
        int actionCount = 1,
        string? equippedGatheringType = null) =>
        new(
            "CombatEncounterCompleted",
            AreaId: areaId,
            WonEncounter: wonEncounter,
            ActionCount: actionCount,
            EquippedGatheringType: equippedGatheringType);

    public static QuestTrigger EssenceAbsorbed(string essenceDefinitionId) =>
        new("EssenceAbsorbed", EssenceDefinitionId: essenceDefinitionId);

    public static QuestTrigger EssenceLoadoutChanged() => new("EssenceLoadoutChanged");

    public static QuestTrigger EssenceFocusSet() => new("EssenceFocusSet");

    public static QuestTrigger EquipmentCrafted(
        IReadOnlyCollection<string> itemBaseIds,
        IReadOnlyCollection<int> tiers) =>
        new("EquipmentCrafted", CraftedItemBaseIds: itemBaseIds, CraftedItemTiers: tiers);

    public static QuestTrigger EquipmentChanged() => new("EquipmentChanged");

    public static QuestTrigger CharacterLevelReached(int level) =>
        new("CharacterLevelReached", CharacterLevel: level);

    public static QuestTrigger ColosseumBattleStarted() => new("ColosseumBattleStarted");

    public static QuestTrigger DailyProphecyCompleted() => new("DailyProphecyCompleted");
}
