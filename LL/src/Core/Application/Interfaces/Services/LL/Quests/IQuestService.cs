using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;

namespace Application.Interfaces.Services.LL.Quests;

public interface IQuestService
{
    Task<QuestJournal> GetJournalAsync(Guid characterId, CancellationToken cancellationToken);
    Task<QuestJournal> AcceptAsync(Guid characterId, string questId, CancellationToken cancellationToken);
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
    int SortOrder,
    QuestStatus Status,
    bool IsPinned,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<QuestObjectiveState> Objectives,
    IReadOnlyList<QuestRewardState> Rewards);

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
    int? CharacterLevel = null)
{
    public static QuestTrigger CombatCompleted(string areaId, bool wonEncounter) =>
        new("CombatEncounterCompleted", AreaId: areaId, WonEncounter: wonEncounter);

    public static QuestTrigger EssenceAbsorbed(string essenceDefinitionId) =>
        new("EssenceAbsorbed", EssenceDefinitionId: essenceDefinitionId);

    public static QuestTrigger EssenceLoadoutChanged() => new("EssenceLoadoutChanged");

    public static QuestTrigger EquipmentCrafted(
        IReadOnlyCollection<string> itemBaseIds,
        IReadOnlyCollection<int> tiers) =>
        new("EquipmentCrafted", CraftedItemBaseIds: itemBaseIds, CraftedItemTiers: tiers);

    public static QuestTrigger EquipmentChanged() => new("EquipmentChanged");

    public static QuestTrigger CharacterLevelReached(int level) =>
        new("CharacterLevelReached", CharacterLevel: level);
}
