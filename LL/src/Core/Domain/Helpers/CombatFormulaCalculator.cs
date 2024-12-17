using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Helpers;
public static class CombatFormulaCalculator
{
    private static readonly Random RandomGenerator = new();

    public static CalculatedResult CalculateCombatInteraction(Entity attacker, Entity defender, int magnitude)
    {
        var calculatedResult = new CalculatedResult();
        int levelDifference = defender.Level - attacker.Level;

        calculatedResult.AttackOutcome = CalculateAttackOutcome(attacker, defender, levelDifference);

        if ((int)calculatedResult.AttackOutcome <= (int)AttackOutcome.Parry) return calculatedResult;

        calculatedResult.CalculatedDamageDealt = CalculateDamageDealt(attacker, magnitude, calculatedResult.AttackOutcome);
        calculatedResult.CalculatedDamageReceived = CalculateDamageReceived(defender, magnitude, calculatedResult.AttackOutcome);

        return calculatedResult;
    }

    private static AttackOutcome CalculateAttackOutcome(Entity attacker, Entity defender, int levelDifference)
    {
        if (!CalculateHit(attacker, defender, levelDifference))
        {
            return AttackOutcome.Miss;
        };

        //if (!CalculateDodge(defender, levelDifference)) return;

        if (CalculateParry(defender))
        {
            return AttackOutcome.Parry;
        };

        if (CalculateBlock(defender))
        {
            return AttackOutcome.Block;
        };

        if (IsCriticalHit(attacker, defender))
        {
            return AttackOutcome.Crit;
        };

        return AttackOutcome.Hit;
    }

    private static bool CalculateHit(Entity attacker, Entity defender, int levelDifference)
    {
        float levelDifferenceModifier = levelDifference / 5 * 3.125f; // Decrease hit chance by 3.125% per level difference
        float statDifferenceModifier = (int)((defender.CombatAttributes[AttributeType.Accuracy] - attacker.CombatAttributes[AttributeType.Dodge]) / 5f) * 1.25f;  // Increased impact from stats
                                                                                                                                                                  // 98                         - ((100-100) / 5 * 3.125) - ((20 - 20) / 5 * 1.25)
        float adjustedHitChance = CombatConstants.BaseHitChance - levelDifferenceModifier - statDifferenceModifier;
        adjustedHitChance = Math.Clamp(adjustedHitChance, CombatConstants.MinHitChance, CombatConstants.MaxHitChance); // Ensure hit chance is between 10% and 100%

        float roll = (float)RandomGenerator.NextDouble() * 100f;
        return roll < adjustedHitChance;
    }

    //private bool CalculateDodge(Entity defender, int levelDifference)
    //{
    //    float baseDodgeChance = 5f;
    //    int deltaLevelDifference = levelDifference / 3;
    //    float dodgeChancePenaltyPer3Levels = 2f; // Decrease hit chance by 2% per level difference
    //    float adjustedDodgeChance = baseDodgeChance + (deltaLevelDifference * dodgeChancePenaltyPer3Levels) + (defender.CombatAttributes[AttributeType.Dodge] / 10);
    //    adjustedDodgeChance = Math.Clamp(adjustedDodgeChance, CombatConstants.MinDodgeChance, CombatConstants.MaxDodgeChance); // Ensure dodge chance is between 0% and 100%

    //    float roll = (float)RandomGenerator.NextDouble() * 100f;
    //    return roll < adjustedDodgeChance;
    //}

    private static bool CalculateParry(Entity defender)
    {
        return false;
    }

    private static bool CalculateBlock(Entity defender)
    {
        return false;
    }

    private static bool IsCriticalHit(Entity attacker, Entity defender)
    {
        var critChance = attacker.CombatAttributes[AttributeType.CritChance];

        float roll = (float)RandomGenerator.NextDouble() * 100f;
        return roll < critChance;
    }

    private static int CalculateDamageDealt(Entity attacker, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * (attacker.CombatAttributes[AttributeType.CritDamage] / 100f));
        return (int)magnitude;
    }

    private static int CalculateDamageReceived(Entity defender, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Block)) return (int)(magnitude * 0.6f);
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * defender.CombatAttributes[AttributeType.CritDamageReduction]);
        return (int)magnitude;
    }
}