namespace Domain.Models.Essences;

public static class CreatureResonanceConstants
{
    public const double GainPerFailedEligibleKill = 1;
    public const int FailedEligibleKillsToMaximumBonus = 12_000;
    // A fraction of the creature's base drop chance: 0.01 means a +1% relative bonus.
    public const double MaximumRelativeDropChanceBonus = 0.01;
    public const double RelativeDropChanceBonusPerPoint = MaximumRelativeDropChanceBonus / FailedEligibleKillsToMaximumBonus;
}
