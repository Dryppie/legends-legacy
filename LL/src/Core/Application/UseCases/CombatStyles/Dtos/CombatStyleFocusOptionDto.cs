using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleFocusOptionDto(Guid PlayerEssenceId, string EssenceDefinitionId, string Name,
    string AbilityId, int CooldownTicks, bool IsEligible, IReadOnlyList<string> EligibleEffectIds);
