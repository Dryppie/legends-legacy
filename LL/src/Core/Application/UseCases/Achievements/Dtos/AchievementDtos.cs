using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Achievements;

namespace Application.UseCases.Achievements.Dtos;

public sealed class AchievementDto : IMapFrom<AchievementDefinition>
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public AchievementCategory Category { get; init; }
    public AchievementType Type { get; init; }
    public AchievementScope Scope { get; init; }
    public AchievementVisibility Visibility { get; init; }
    public TitleRarity Rarity { get; init; }
    public AchievementRequirementType RequirementType { get; init; }
    public string? RequirementTarget { get; init; }
    public long RequiredAmount { get; init; }
    public long CurrentAmount { get; init; }
    public int Points { get; init; }
    public bool IsCompleted { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public Guid? CompletedByCharacterId { get; init; }
    public string? RewardTitleKey { get; init; }
    public string? RewardTitleName { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AchievementDefinition, AchievementDto>()
            .ForMember(dest => dest.RequiredAmount, opt => opt.MapFrom(src => src.RequirementAmount))
            .ForMember(dest => dest.CurrentAmount, opt => opt.Ignore())
            .ForMember(dest => dest.IsCompleted, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedByCharacterId, opt => opt.Ignore())
            .ForMember(dest => dest.RewardTitleKey, opt => opt.Ignore())
            .ForMember(dest => dest.RewardTitleName, opt => opt.Ignore());
    }
}

public sealed class AchievementOverviewDto
{
    public int TotalAchievementPoints { get; init; }
    public int LegacyRenownRank { get; init; }
    public string LegacyRenownName { get; init; } = string.Empty;
    public int TotalAchievementsUnlocked { get; init; }
    public int TotalAchievementsAvailable { get; init; }
    public int TotalTitlesUnlocked { get; init; }
    public IReadOnlyList<AchievementDto> RecentlyUnlockedAchievements { get; init; } = [];
    public IReadOnlyList<AchievementDto> NearlyCompletedAchievements { get; init; } = [];
    public IReadOnlyList<AchievementCategorySummaryDto> CategorySummaries { get; init; } = [];
}

public sealed class AchievementCategorySummaryDto
{
    public AchievementCategory Category { get; init; }
    public int Unlocked { get; init; }
    public int Available { get; init; }
    public long CurrentProgress { get; init; }
    public long RequiredProgress { get; init; }
}

public sealed class TitleDto : IMapFrom<TitleDefinition>
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AchievementCategory Category { get; init; }
    public TitleRarity Rarity { get; init; }
    public TitleDisplayPosition DisplayPosition { get; init; }
    public TitleScope Scope { get; init; }
    public bool IsUnlocked { get; init; }
    public bool IsEquipped { get; init; }
    public string? SourceAchievementKey { get; init; }
    public Guid? UnlockedByCharacterId { get; init; }
    public DateTimeOffset? UnlockedAt { get; init; }
    public string Preview { get; init; } = string.Empty;
    public string PrefixPreview { get; init; } = string.Empty;
    public string SuffixPreview { get; init; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TitleDefinition, TitleDto>()
            .ForMember(dest => dest.DisplayPosition, opt => opt.Ignore())
            .ForMember(dest => dest.IsUnlocked, opt => opt.Ignore())
            .ForMember(dest => dest.IsEquipped, opt => opt.Ignore())
            .ForMember(dest => dest.UnlockedByCharacterId, opt => opt.Ignore())
            .ForMember(dest => dest.UnlockedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Preview, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.PrefixPreview, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.SuffixPreview, opt => opt.MapFrom(src => src.Name));
    }
}

public sealed class EquippedTitleDto : IMapFrom<TitleDefinition>
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public TitleDisplayPosition DisplayPosition { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TitleDefinition, EquippedTitleDto>()
            .ForMember(dest => dest.DisplayPosition, opt => opt.Ignore())
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.Name));
    }
}

public sealed class AchievementUnlockDto : IMapFrom<PlayerAchievementProgress>
{
    public Guid UnlockId { get; init; }
    public string AchievementKey { get; init; } = string.Empty;
    public string AchievementName { get; init; } = string.Empty;
    public int Points { get; init; }
    public string? TitleKey { get; init; }
    public string? TitleName { get; init; }
    public bool ShouldAnnounce { get; init; }
    public string? PlayerSystemMessage { get; init; }
    public string? GlobalSystemMessage { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PlayerAchievementProgress, AchievementUnlockDto>()
            .ForMember(dest => dest.UnlockId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AchievementKey, opt => opt.MapFrom(src => src.AchievementDefinition.Key))
            .ForMember(dest => dest.AchievementName, opt => opt.MapFrom(src => src.AchievementDefinition.Name))
            .ForMember(dest => dest.Points, opt => opt.MapFrom(src => src.AchievementDefinition.Points))
            .ForMember(
                dest => dest.ShouldAnnounce,
                opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.AchievementDefinition.GlobalSystemMessageTemplate)))
            .ForMember(dest => dest.TitleKey, opt => opt.Ignore())
            .ForMember(dest => dest.TitleName, opt => opt.Ignore())
            .ForMember(dest => dest.PlayerSystemMessage, opt => opt.Ignore())
            .ForMember(dest => dest.GlobalSystemMessage, opt => opt.Ignore());
    }
}

public sealed class AchievementRecalculationResultDto
{
    public Guid AccountId { get; init; }
    public Guid CharacterId { get; init; }
    public int CompletedBefore { get; init; }
    public int CompletedAfter { get; init; }
    public int NewlyCompleted => Math.Max(0, CompletedAfter - CompletedBefore);
    public IReadOnlyList<AchievementUnlockDto> Unlocks { get; init; } = [];
}
