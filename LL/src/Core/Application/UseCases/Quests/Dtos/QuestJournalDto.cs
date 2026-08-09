using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Quests;

namespace Application.UseCases.Quests.Dtos;

public sealed class QuestJournalDto : IMapFrom<QuestJournal>
{
    public IReadOnlyList<QuestStateDto> Quests { get; set; } = [];
    public string? PinnedQuestId { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<QuestJournal, QuestJournalDto>();
        profile.CreateMap<QuestState, QuestStateDto>();
        profile.CreateMap<QuestChain, QuestChainDto>();
        profile.CreateMap<QuestChoice, QuestChoiceDto>();
        profile.CreateMap<QuestChoiceOption, QuestChoiceOptionDto>();
        profile.CreateMap<QuestObjectiveState, QuestObjectiveStateDto>();
        profile.CreateMap<QuestRewardState, QuestRewardStateDto>();
        profile.CreateMap<QuestPresentation, QuestPresentationDto>();
    }
}

public sealed class QuestStateDto
{
    public string QuestId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ObjectiveMode { get; set; } = string.Empty;
    public QuestChainDto? Chain { get; set; }
    public QuestChoiceDto? Choice { get; set; }
    public int SortOrder { get; set; }
    public QuestStatus Status { get; set; }
    public bool IsPinned { get; set; }
    public bool RequiresWelcome { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public IReadOnlyList<QuestObjectiveStateDto> Objectives { get; set; } = [];
    public IReadOnlyList<QuestRewardStateDto> Rewards { get; set; } = [];
}

public sealed class QuestChoiceDto
{
    public string SelectionTitle { get; set; } = string.Empty;
    public string SelectionSummary { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
    public string? SelectedOptionKey { get; set; }
    public IReadOnlyList<QuestChoiceOptionDto> Options { get; set; } = [];
}

public sealed class QuestChoiceOptionDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid CreatureId { get; set; }
    public string CreatureName { get; set; } = string.Empty;
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public string RewardItemBaseId { get; set; } = string.Empty;
    public string EncounterKey { get; set; } = string.Empty;
    public ItemBaseDto? RewardItemBase { get; set; }
}

public sealed class QuestChainDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Step { get; set; }
    public int TotalSteps { get; set; }
}

public sealed class QuestObjectiveStateDto
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long CurrentAmount { get; set; }
    public long RequiredAmount { get; set; }
    public bool IsCompleted { get; set; }
    public QuestPresentationDto Presentation { get; set; } = new();
}

public sealed class QuestRewardStateDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ItemBaseId { get; set; }
    public int Quantity { get; set; }
    public ItemBaseDto? ItemBase { get; set; }
}

public sealed class QuestPresentationDto
{
    public string ActionLabel { get; set; } = string.Empty;
    public string DestinationRoute { get; set; } = string.Empty;
    public string? GuidePageId { get; set; }
    public string? TourPageId { get; set; }
}
