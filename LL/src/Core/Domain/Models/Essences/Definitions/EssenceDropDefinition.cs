namespace Domain.Models.Essences.Definitions;

public sealed class EssenceDropDefinition
{
    public double BaseDropChance { get; set; } = 0.001;
    public double ResonanceGainPerFailedEligibleKill { get; set; } = 1;
    public double DropChanceBonusPerResonance { get; set; } = 0.00005;
    public double MaxResonanceBonus { get; set; } = 0.01;
}
