using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleOverviewDto(string ContentVersion, IReadOnlyList<CombatStyleEntryDto> Styles,
    CombatStyleSelectionDto Selection, CombatStyleSnapshotDto? EffectiveStyle, string? ValidationIssue,
    IReadOnlyList<CombatStylePreviewFactDto> PreviewFacts);
