using Domain.Models.Items;
using Domain.Models.Quests.Events;

namespace Application.Interfaces.Services.LL.Quests.Events;

public interface IEventQuestService
{
    Task<EventQuestJournal> GetJournalAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EventQuestJournal> ClaimAsync(Guid characterId, string eventQuestId, CancellationToken cancellationToken);
    Task<EventQuestJournal> ClaimMilestoneAsync(
        Guid characterId,
        string eventQuestId,
        string milestoneKey,
        CancellationToken cancellationToken);
    Task<EventQuestJournal> ClaimAllMilestonesAsync(
        Guid characterId,
        string eventQuestId,
        CancellationToken cancellationToken);
}

public interface IEventQuestProgressionService
{
    Task ProcessAsync(Guid characterId, QuestTrigger trigger, Guid outboxMessageId, string eventType, CancellationToken cancellationToken);
}

public sealed record EventQuestJournal(IReadOnlyList<EventQuestState> Events);

public sealed record EventQuestState(
    string EventQuestId,
    int Version,
    string Title,
    string Summary,
    EventQuestStatus Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset ClaimEndsAtUtc,
    DateTimeOffset? CompletedAt,
    long MinimumContribution,
    long MyContribution,
    bool IsEligible,
    bool HasClaimed,
    int? MyContributionRank,
    int ContributorCount,
    long? ContributionToNextRank,
    int SortOrder,
    IReadOnlyList<EventQuestObjectiveState> Objectives,
    IReadOnlyList<EventQuestRewardState> Rewards,
    IReadOnlyList<EventQuestPersonalMilestoneState> PersonalMilestones,
    IReadOnlyList<EventQuestContributorState> TopContributors);

public sealed record EventQuestObjectiveState(
    string Key,
    string Description,
    string Type,
    long CurrentAmount,
    long RequiredAmount,
    bool IsCompleted);

public sealed record EventQuestRewardState(
    string Key,
    string Type,
    string? ItemBaseId,
    int Quantity,
    ItemBase? ItemBase);

public sealed record EventQuestPersonalMilestoneState(
    string Key,
    long RequiredContribution,
    bool IsUnlocked,
    bool IsClaimed,
    IReadOnlyList<EventQuestRewardState> Rewards);

public sealed record EventQuestContributorState(
    int Rank,
    Guid CharacterId,
    string CharacterName,
    long Contribution);
