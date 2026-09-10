using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleEntryDto(CombatStyleDefinitionDto Definition, int Level, long CurrentXp,
    long XpRequired, int UpgradeSlots, string? RefinementId, IReadOnlyList<string> UpgradeIds,
    string? MasteredUpgradeId);
