namespace Domain.Models.Essences;

public static class CreatureResonanceConstants
{
    public const double GainPerFailedEligibleKill = 1;
    public const int FailedEligibleKillsToMaximumBonus = 12_000;
    public const double MaximumDropChanceBonus = 0.01;
    public const double DropChanceBonusPerPoint = MaximumDropChanceBonus / FailedEligibleKillsToMaximumBonus;
}
