namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceDropDto(double BaseDropChance, double ResonanceGainPerFailedEligibleKill, double DropChanceBonusPerResonance, double MaxResonanceBonus);
