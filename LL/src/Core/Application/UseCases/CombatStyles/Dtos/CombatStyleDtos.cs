using Application.Common.Mappings;
using Application.Interfaces.Services.LL.CombatStyles;
using Application.UseCases.CombatStyles.Models;
using AutoMapper;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed class CombatStylesOverviewDto : IMapFrom<CombatStylesOverviewModel>
{
    public string? ActiveStyleId { get; set; }
    public IReadOnlyList<CombatStyleDto> Styles { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStylesOverviewModel, CombatStylesOverviewDto>();
    }
}

public sealed class CombatStyleDto : IMapFrom<CombatStyleModel>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string CoreMechanic { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceForCurrentLevel { get; set; }
    public long ExperienceForNextLevel { get; set; }
    public bool IsActive { get; set; }
    public string? SelectedFocusId { get; set; }
    public int SkillPointsEarned { get; set; }
    public int SkillPointsSpent { get; set; }
    public int SkillPointsAvailable { get; set; }
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public IReadOnlyList<CombatStyleFocusDto> Focuses { get; set; } = [];
    public CombatStyleSkillTreeDto SkillTree { get; set; } = new();
    public IReadOnlyList<CombatStyleRuleSummaryDto> RuleSummaries { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleModel, CombatStyleDto>();
    }
}

public sealed class CombatStyleFocusDto : IMapFrom<CombatStyleFocusModel>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UnlockLevel { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsSelected { get; set; }
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleFocusModel, CombatStyleFocusDto>();
    }
}

public sealed class CombatStyleRuleSummaryDto : IMapFrom<CombatStyleRuleSummaryModel>
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleRuleSummaryModel, CombatStyleRuleSummaryDto>();
    }
}

public sealed class CombatStyleSkillTreeDto : IMapFrom<CombatStyleSkillTreeModel>
{
    public IReadOnlyList<CombatStyleSkillTreeBranchDto> Branches { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleSkillTreeModel, CombatStyleSkillTreeDto>();
    }
}

public sealed class CombatStyleSkillTreeBranchDto : IMapFrom<CombatStyleSkillTreeBranchModel>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public int PointsSpent { get; set; }
    public IReadOnlyList<CombatStyleSkillTreeNodeDto> Nodes { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleSkillTreeBranchModel, CombatStyleSkillTreeBranchDto>();
    }
}

public sealed class CombatStyleSkillTreeNodeDto : IMapFrom<CombatStyleSkillTreeNodeModel>
{
    public string Id { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int MaxRank { get; set; }
    public int RequiredLevel { get; set; }
    public string? RequiredNodeId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsUnlocked { get; set; }
    public bool CanRankUp { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
    public IReadOnlyList<string> Effects { get; set; } = [];
    public int Row { get; set; }
    public string Lane { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? MutatorKind { get; set; }
    public IReadOnlyList<string> MutatorGroups { get; set; } = [];
    public CombatStyleNodeTooltipDto Tooltip { get; set; } = new();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleSkillTreeNodeModel, CombatStyleSkillTreeNodeDto>();
    }
}

public sealed class CombatStyleNodeTooltipDto : IMapFrom<CombatStyleNodeTooltipModel>
{
    public IReadOnlyList<string> Affects { get; set; } = [];
    public IReadOnlyList<string> Changes { get; set; } = [];
    public IReadOnlyList<string> Tradeoffs { get; set; } = [];
    public IReadOnlyList<string> DoesNotAffect { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleNodeTooltipModel, CombatStyleNodeTooltipDto>();
    }
}

public sealed class ActivateCombatStyleResponseDto : IMapFrom<CombatStyleOperationResult>
{
    public bool Success { get; set; }
    public string ActiveStyleId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleOperationResult, ActivateCombatStyleResponseDto>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.Succeeded))
            .ForMember(dest => dest.ActiveStyleId, opt => opt.MapFrom(src => src.ActiveStyleId ?? string.Empty));
    }
}

public sealed class CombatStyleMutationResponseDto : IMapFrom<CombatStyleOperationResult<CombatStyleModel>>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CombatStyleDto? Style { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatStyleOperationResult<CombatStyleModel>, CombatStyleMutationResponseDto>()
            .ConvertUsing((src, _, context) => new CombatStyleMutationResponseDto
            {
                Success = src.Succeeded,
                Message = src.Message,
                Style = src.Value is null ? null : context.Mapper.Map<CombatStyleDto>(src.Value)
            });
    }
}

public sealed class CombatBuildPreviewDto : IMapFrom<CombatBuildPreviewModel>
{
    public string ActiveStyleId { get; set; } = string.Empty;
    public string ActiveStyleName { get; set; } = string.Empty;
    public string? SelectedFocusId { get; set; }
    public string? SelectedFocusName { get; set; }
    public string BuildName { get; set; } = string.Empty;
    public IReadOnlyList<TagScoreDto> TopTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public IReadOnlyList<string> Notes { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CombatBuildPreviewModel, CombatBuildPreviewDto>();
    }
}

public sealed class TagScoreDto : IMapFrom<TagScoreModel>
{
    public string Tag { get; set; } = string.Empty;
    public int Score { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TagScoreModel, TagScoreDto>();
    }
}
