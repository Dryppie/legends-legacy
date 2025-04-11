using Domain.Helpers.Constants;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Helpers;
public static class CombatFormulaCalculator
{
    private static readonly Random RandomGenerator = new();

    public static AttackOutcome CalculateAttackOutcome(CombatEntity attacker, CombatEntity defender, List<EffectModification> effectModifications, bool isDamage)
    {
        int levelDifference = defender.Level - attacker.Level;

        return CalculateAttackOutcome(attacker, defender, effectModifications, levelDifference, isDamage);
    }

    private static AttackOutcome CalculateAttackOutcome(CombatEntity attacker, CombatEntity defender, List<EffectModification> effectModifications, int levelDifference, bool isDamage)
    {
        if (!isDamage) // If it's healing, we either crit or hit.
        {
            if (IsCriticalHit(attacker, defender, effectModifications)) return AttackOutcome.Crit;
            return AttackOutcome.Hit;
        }

        if (!CalculateHit(attacker, defender, levelDifference))
        {
            return AttackOutcome.Miss;
        };

        //if (!CalculateDodge(defender, levelDifference)) return;

        if (IsParry(defender))
        {
            return AttackOutcome.Parry;
        };

        if (IsBlock(defender))
        {
            return AttackOutcome.Block;
        };

        if (IsCriticalHit(attacker, defender, effectModifications))
        {
            return AttackOutcome.Crit;
        };

        return AttackOutcome.Hit;
    }

    private static bool CalculateHit(CombatEntity attacker, CombatEntity defender, int levelDifference)
    {
        float levelDifferenceModifier = levelDifference / 5 * 3.125f; // Decrease hit chance by 3.125% per level difference
        float statDifferenceModifier = (int)((defender.CombatAttributes[AttributeType.Accuracy] - attacker.CombatAttributes[AttributeType.Dodge]) / 5f) * 1.25f;  // Increased impact from stats
                                                                                                                                                                  // 98                         - ((100-100) / 5 * 3.125) - ((20 - 20) / 5 * 1.25)
        float adjustedHitChance = CombatConstants.BASE_HIT_CHANCE + statDifferenceModifier - levelDifferenceModifier;
        adjustedHitChance = Math.Clamp(adjustedHitChance, CombatConstants.MIN_HIT_CHANCE, CombatConstants.MAX_HIT_CHANCE); // Ensure hit chance is between 10% and 100%

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

    private static bool IsParry(CombatEntity defender)
    {
        var parryChance = defender.GetAttributeValue(AttributeType.Parry) * CombatConstants.BASE_PARRY_VALUE;
        var adjustedParryChance = Math.Min(parryChance, CombatConstants.MAX_PARRY_CHANCE);

        float roll = (float)RandomGenerator.NextDouble() * 100f;
        return roll < adjustedParryChance;
    }

    private static bool IsBlock(CombatEntity defender)
    {
        var blockChance = defender.GetAttributeValue(AttributeType.Parry) * CombatConstants.BASE_BLOCK_VALUE;
        var adjustedBlockChance = Math.Min(blockChance, CombatConstants.MAX_BLOCK_CHANCE);

        float roll = (float)RandomGenerator.NextDouble() * 100f;
        return roll < adjustedBlockChance;
    }

    private static bool IsCriticalHit(CombatEntity attacker, CombatEntity defender, List<EffectModification> effectModifications)
    {
        var critChance = attacker.CombatAttributes[AttributeType.CritChance];
        var modifications = effectModifications.Where(em => em.EffectModificationType.Equals(EffectModificationType.CritChance)).ToList();

        var modifiedCritChance = GetModifiedValue(modifications, critChance);

        float roll = (float)RandomGenerator.NextDouble() * 100f;
        return roll < modifiedCritChance;
    }

    private static int CalculateDamageDealt(CombatEntity attacker, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * (attacker.CombatAttributes[AttributeType.CritDamage] / 100f));
        return (int)magnitude;
    }

    private static int CalculateDamageReceived(CombatEntity defender, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Block)) return (int)(magnitude * 0.6f);
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * defender.CombatAttributes[AttributeType.CritDamageReduction]);
        return (int)magnitude;
    }

    private static float GetModifiedValue(List<EffectModification> effectModifications, float baseValue)
    {
        if (effectModifications.Count == 0) return baseValue;

        float flatSum = 0f;
        float additiveSum = 0f;
        float multiplicativeProduct = 1f;

        // Iterate through each modifier once and calculate sums and product
        foreach (var modifier in effectModifications)
        {
            switch (modifier.ModifierType)
            {
                case ModifierType.Flat:
                    flatSum += modifier.Amount;
                    break;
                case ModifierType.Additive:
                    additiveSum += modifier.Amount / 100f;
                    break;
                case ModifierType.Multiplicative:
                    multiplicativeProduct *= (1 + modifier.Amount / 100f);
                    break;
            }
        }

        // Return the final rounded attribute value
        float result = MathF.Round((baseValue + flatSum) * (1 + additiveSum) * multiplicativeProduct, MidpointRounding.ToZero);
        return Math.Max(result, 0);
    }
}