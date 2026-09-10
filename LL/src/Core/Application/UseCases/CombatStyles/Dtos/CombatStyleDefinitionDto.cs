using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleDefinitionDto(string Id, string Name, string Description,
    CombatStyleKind Kind, CombatStyleTuningDto Tuning, IReadOnlyList<CombatStyleChoiceDto> Refinements, IReadOnlyList<CombatStyleChoiceDto> Upgrades,
    CombatStyleOpeningTechniqueDto? OpeningTechnique, CombatStyleMilestoneTuning MilestoneTuning);
