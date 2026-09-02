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
    string Description,
    string Goal,
    string PromisedReward,
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
    IReadOnlyList<InventoryItem> Loot,
    bool JournalChanged = true);

public sealed record QuestTrigger(
    string Type,
    string? AreaId = null,
    string? DungeonDefinitionId = null,
    bool? WonEncounter = null,
    string? EssenceDefinitionId = null,
    IReadOnlyCollection<string>? CraftedItemBaseIds = null,
    IReadOnlyCollection<int>? CraftedItemTiers = null,
    IReadOnlyCollection<string?>? CraftedBaseRecipeIds = null,
    IReadOnlyCollection<ItemQuality>? CraftedItemQualities = null,
    IReadOnlyCollection<int?>? CraftedItemPotentials = null,
    int? CharacterLevel = null,
    int ActionCount = 1,
    string? EquippedGatheringType = null,
    bool HasCompatibleEssenceTrio = false,
    string? CreatureDefinitionId = null,
    int? WinningEncounterCount = null,
    int GatheredResourceCount = 0)
{
    public static QuestTrigger CombatCompleted(
        string areaId,
        bool wonEncounter,
        int actionCount = 1,
        string? equippedGatheringType = null,
        int? winningEncounterCount = null,
        int gatheredResourceCount = 0) =>
        new(
            "CombatEncounterCompleted",
            AreaId: areaId,
            WonEncounter: wonEncounter,
            ActionCount: actionCount,
            EquippedGatheringType: equippedGatheringType,
            WinningEncounterCount: winningEncounterCount,
            GatheredResourceCount: gatheredResourceCount);

    public static QuestTrigger EssenceAbsorbed(string essenceDefinitionId) =>
        new("EssenceAbsorbed", EssenceDefinitionId: essenceDefinitionId);

    public static QuestTrigger EssenceLoadoutChanged(bool hasCompatibleEssenceTrio = false) =>
        new("EssenceLoadoutChanged", HasCompatibleEssenceTrio: hasCompatibleEssenceTrio);

    public static QuestTrigger EssenceFocusSet() => new("EssenceFocusSet");

    public static QuestTrigger FocusedCreatureEssenceReceived(
        string creatureDefinitionId,
        string essenceDefinitionId) =>
        new(
            "FocusedCreatureEssenceReceived",
            EssenceDefinitionId: essenceDefinitionId,
            CreatureDefinitionId: creatureDefinitionId);

    public static QuestTrigger EssenceAscended() => new("EssenceAscended");

    public static QuestTrigger EquipmentCrafted(
        IReadOnlyCollection<string> itemBaseIds,
        IReadOnlyCollection<int> tiers,
        IReadOnlyCollection<string?>? baseRecipeIds = null,
        IReadOnlyCollection<ItemQuality>? qualities = null,
        IReadOnlyCollection<int?>? potentials = null) =>
        new(
            "EquipmentCrafted",
            CraftedItemBaseIds: itemBaseIds,
            CraftedItemTiers: tiers,
            CraftedBaseRecipeIds: baseRecipeIds,
            CraftedItemQualities: qualities,
            CraftedItemPotentials: potentials);

    public static QuestTrigger EquipmentTempered(
        IReadOnlyCollection<string> itemBaseIds,
        IReadOnlyCollection<int> tiers,
        IReadOnlyCollection<string?> baseRecipeIds,
        IReadOnlyCollection<ItemQuality> qualities,
        IReadOnlyCollection<int?> potentials,
        int actionCount = 1) =>
        new(
            "EquipmentTempered",
            CraftedItemBaseIds: itemBaseIds,
            CraftedItemTiers: tiers,
            CraftedBaseRecipeIds: baseRecipeIds,
            CraftedItemQualities: qualities,
            CraftedItemPotentials: potentials,
            ActionCount: actionCount);

    public static QuestTrigger EquipmentChanged() => new("EquipmentChanged");

    public static QuestTrigger CharacterLevelReached(int level) =>
        new("CharacterLevelReached", CharacterLevel: level);

    public static QuestTrigger ColosseumBattleStarted() => new("ColosseumBattleStarted");

    public static QuestTrigger TournamentBattleCompleted() => new("TournamentBattleCompleted");

    public static QuestTrigger DungeonRunStarted() => new("DungeonRunStarted");

    public static QuestTrigger DungeonRunCompleted(string? dungeonDefinitionId = null) =>
        new("DungeonRunCompleted", DungeonDefinitionId: dungeonDefinitionId);

    public static QuestTrigger DailyProphecyCompleted() => new("DailyProphecyCompleted");
}
