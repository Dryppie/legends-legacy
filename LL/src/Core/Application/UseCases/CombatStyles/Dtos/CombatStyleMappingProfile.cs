using AutoMapper;
using Domain.Models.CombatStyles;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed class CombatStyleMappingProfile : Profile
{
    public CombatStyleMappingProfile()
    {
        CreateMap<CombatStyleOverview, CombatStyleOverviewDto>();
        CreateMap<CombatStyleEntry, CombatStyleEntryDto>();
        CreateMap<CombatStyleDefinition, CombatStyleDefinitionDto>();
        CreateMap<CombatStyleTuning, CombatStyleTuningDto>();
        CreateMap<ReaperTuning, ReaperTuningDto>();
        CreateMap<CombatStyleSnapshot, CombatStyleSnapshotDto>();
        CreateMap<CombatStyleChoiceDefinition, CombatStyleChoiceDto>();
        CreateMap<CombatStyleOpeningTechnique, CombatStyleOpeningTechniqueDto>();
        CreateMap<CombatStylePreviewFact, CombatStylePreviewFactDto>();
        CreateMap<CombatStyleSelectionRequest, CombatStyleSelectionDto>();
        CreateMap<CombatStyleSelectionDto, CombatStyleSelectionRequest>().ConvertUsing(source =>
            new CombatStyleSelectionRequest(source.CombatStyleId, source.RefinementId,
                source.UpgradeIds ?? Array.Empty<string>(), null, source.RestoreRememberedChoices, source.MasteredUpgradeId));
    }
}
