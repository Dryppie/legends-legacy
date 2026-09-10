using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleSelectionDto(
    string? CombatStyleId = null, string? RefinementId = null, IReadOnlyList<string>? UpgradeIds = null,
    bool RestoreRememberedChoices = false, string? MasteredUpgradeId = null);
