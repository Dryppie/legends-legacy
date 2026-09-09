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
        CreateMap<CombatStyleChoiceDefinition, CombatStyleChoiceDto>();
        CreateMap<CombatStyleOpeningTechnique, CombatStyleOpeningTechniqueDto>();
        CreateMap<CombatStyleFocusOption, CombatStyleFocusOptionDto>();
        CreateMap<CombatStylePreviewFact, CombatStylePreviewFactDto>();
        CreateMap<CombatStyleSelectionRequest, CombatStyleSelectionDto>();
        CreateMap<CombatStyleSelectionDto, CombatStyleSelectionRequest>().ConvertUsing(source =>
            new CombatStyleSelectionRequest(source.CombatStyleId, source.RefinementId,
                source.UpgradeIds ?? Array.Empty<string>(), source.FocusPlayerEssenceId, source.RestoreRememberedChoices, source.MasteredUpgradeId));
    }
}
