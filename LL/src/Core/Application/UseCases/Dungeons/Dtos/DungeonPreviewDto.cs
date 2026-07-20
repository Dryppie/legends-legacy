using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonHubDto
{
    public long SigilFragments { get; set; }
    public bool SigilAssemblyEnabled { get; set; }
    public int SigilAssemblyCost { get; set; }
    public List<DungeonPreviewDto> Dungeons { get; set; } = [];
}

public sealed class DungeonPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public string FamilyTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int RecommendedCombatRating { get; set; }
    public int CurrentCombatRating { get; set; }
    public bool CanEnter { get; set; }
    public List<string> MissingRequirements { get; set; } = [];
    public List<DungeonEntryRequirementDto> EntryRequirements { get; set; } = [];
    public string? SigilItemId { get; set; }
    public string? SigilName { get; set; }
    public bool CanAssembleSigil { get; set; }
    public List<string> SigilAssemblyMissingRequirements { get; set; } = [];
    public string? RequiredPreviousDungeonId { get; set; }
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public DungeonTier DungeonTier { get; set; }
    public DungeonRecordDto Record { get; set; } = new();
    public DungeonMasteryDto Mastery { get; set; } = new();
    public List<DungeonPreviewRewardDto> Rewards { get; set; } = [];
    public List<DungeonGatheringNodePreviewDto> GatheringNodes { get; set; } = [];
}

public sealed class DungeonMasteryDto : IMapFrom<DungeonMasterySnapshot>
{
    public long Experience { get; set; }
    public int Level { get; set; }
    public int? ExperienceRequiredForNextLevel { get; set; }
    public int CompletionCount { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonMasterySnapshot, DungeonMasteryDto>();
}

public sealed class DungeonPreviewRewardDto : IMapFrom<DungeonPreviewReward>
{
    public string Id { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public double? DropChancePercent { get; set; }
    public bool CanDropNothing { get; set; }
    public double? NoDropChancePercent { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonPreviewReward, DungeonPreviewRewardDto>()
            .ForMember(
                destination => destination.Id,
                options => options.MapFrom(source => source.ItemBase.Id));
    }
}

public sealed class DungeonEntryRequirementDto : IMapFrom<DungeonEntryRequirementResult>
{
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RequiredAmount { get; set; }
    public int OwnedAmount { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonEntryRequirementResult, DungeonEntryRequirementDto>();
}

public sealed class DungeonGatheringNodePreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? LevelRequirement { get; set; }
    public float ProcChance { get; set; }
    public List<DungeonGatheringLootPreviewDto> Loot { get; set; } = [];
}

public sealed class DungeonGatheringLootPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRare { get; set; }
}
