using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Quests.Events;

namespace Application.UseCases.Quests.Events.Dtos;

public sealed class EventQuestJournalDto : IMapFrom<EventQuestJournal>
{
    public IReadOnlyList<EventQuestStateDto> Events { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EventQuestJournal, EventQuestJournalDto>();
        profile.CreateMap<EventQuestState, EventQuestStateDto>();
        profile.CreateMap<EventQuestObjectiveState, EventQuestObjectiveStateDto>();
        profile.CreateMap<EventQuestRewardState, EventQuestRewardStateDto>();
        profile.CreateMap<EventQuestPersonalMilestoneState, EventQuestPersonalMilestoneStateDto>();
        profile.CreateMap<EventQuestContributorState, EventQuestContributorStateDto>();
    }
}

public sealed class EventQuestStateDto
{
    public string EventQuestId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public EventQuestStatus Status { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DateTimeOffset ClaimEndsAtUtc { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long MinimumContribution { get; set; }
    public long MyContribution { get; set; }
    public bool IsEligible { get; set; }
    public bool HasClaimed { get; set; }
    public int? MyContributionRank { get; set; }
    public int ContributorCount { get; set; }
    public long? ContributionToNextRank { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<EventQuestObjectiveStateDto> Objectives { get; set; } = [];
    public IReadOnlyList<EventQuestRewardStateDto> Rewards { get; set; } = [];
    public IReadOnlyList<EventQuestPersonalMilestoneStateDto> PersonalMilestones { get; set; } = [];
    public IReadOnlyList<EventQuestContributorStateDto> TopContributors { get; set; } = [];
}

public sealed class EventQuestObjectiveStateDto
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long CurrentAmount { get; set; }
    public long RequiredAmount { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class EventQuestRewardStateDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ItemBaseId { get; set; }
    public int Quantity { get; set; }
    public ItemBaseDto? ItemBase { get; set; }
}

public sealed class EventQuestPersonalMilestoneStateDto
{
    public string Key { get; set; } = string.Empty;
    public long RequiredContribution { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsClaimed { get; set; }
    public IReadOnlyList<EventQuestRewardStateDto> Rewards { get; set; } = [];
}

public sealed class EventQuestContributorStateDto
{
    public int Rank { get; set; }
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public long Contribution { get; set; }
}
