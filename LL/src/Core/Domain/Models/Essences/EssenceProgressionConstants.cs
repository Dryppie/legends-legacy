using Domain.Models.AbilityDefinitions;

namespace Domain.Models.Essences;

public static class EssenceProgressionConstants
{
    public const int BaseXpPerLevel = 100;
    public const double XpGrowth = 1.18;
    public const int MaxEssenceLevel = 60;
    public const string LesserMonsterCoreItemId = "item.monster_core.lesser";
    public const string GreaterMonsterCoreItemId = "item.monster_core.greater";
    public const string PrimalMonsterCoreItemId = "item.monster_core.primal";
    public const int TierOneMonsterCoreCost = 6;
    public const int TierTwoMonsterCoreCost = 12;
    public const int TierThreeMonsterCoreCost = 24;
    public const int TierOneCatchUpThreshold = 10;
    public const int TierTwoCatchUpThreshold = 10;
    public const int TierOneCatchUpMonsterCoreCost = 3;
    public const int TierTwoCatchUpMonsterCoreCost = 8;
    public const double AttributeBonusGrowthPerLevel = 0.04;
    public const double ActiveCooldownReductionPerAscensionTier = 0.05;
    public const double MaxActiveCooldownReduction = 0.15;
    public const double DamageValueGrowthPerAscensionTier = 0.12;
    public const double HealingValueGrowthPerAscensionTier = 0.10;
    public const double BarrierValueGrowthPerAscensionTier = 0.10;
    public const double AttributeValueGrowthPerAscensionTier = 0.08;
    public const double StatusStackValueGrowthPerAscensionTier = 0.06;
    public const double DurationGrowthPerAscensionTier = 0.05;
    public const double MaxDurationGrowth = 0.15;
    public const double SummonPowerGrowthPerAscensionTier = 0.12;
    public const double SummonHealthGrowthPerAscensionTier = 0.12;
    public const double MinimumCooldownSeconds = 1;

    public static int GetXpRequiredForLevel(int level)
    {
        if (level >= MaxEssenceLevel) return 0;
        return (int)Math.Ceiling(BaseXpPerLevel * Math.Pow(XpGrowth, Math.Max(0, level - 1)));
    }

    public static int GetLevelCap(int ascensionTier) => ascensionTier switch
    {
        <= 0 => 10,
        1 => 30,
        _ => 60
    };

    public static EssenceAscensionCost GetAscensionCost(
        int nextAscensionTier,
        int ascendedToTierOneCount = 0,
        int ascendedToTierTwoCount = 0)
    {
        return nextAscensionTier switch
        {
            1 => new(
                LesserMonsterCoreItemId,
                ascendedToTierOneCount >= TierOneCatchUpThreshold
                    ? TierOneCatchUpMonsterCoreCost
                    : TierOneMonsterCoreCost),
            2 => new(
                GreaterMonsterCoreItemId,
                ascendedToTierTwoCount >= TierTwoCatchUpThreshold
                    ? TierTwoCatchUpMonsterCoreCost
                    : TierTwoMonsterCoreCost),
            3 => new(PrimalMonsterCoreItemId, TierThreeMonsterCoreCost),
            _ => throw new ArgumentOutOfRangeException(nameof(nextAscensionTier), "Ascension tier must be between 1 and 3.")
        };
    }

    public static double ScaleAbilityValue(double baseValue, int level, int ascensionTier = 0, string? effectType = null)
    {
        var multiplier = 1 + GetTierValueGrowth(effectType) * Math.Max(0, ascensionTier);
        return baseValue * multiplier;
    }

    public static double ScaleAttributeBonus(double baseValue, int level)
    {
        var multiplier = 1 + AttributeBonusGrowthPerLevel * Math.Max(0, level - 1);
        return baseValue * multiplier;
    }

    public static double ScaleActiveCooldownSeconds(double baseCooldownSeconds, int ascensionTier)
    {
        if (baseCooldownSeconds <= MinimumCooldownSeconds) return baseCooldownSeconds;

        var reduction = Math.Min(MaxActiveCooldownReduction, ActiveCooldownReductionPerAscensionTier * Math.Max(0, ascensionTier));
        return Math.Max(MinimumCooldownSeconds, baseCooldownSeconds * (1 - reduction));
    }

    public static double ScaleEffectDurationSeconds(double baseDurationSeconds, int ascensionTier, string? effectType, string? statusId)
    {
        if (baseDurationSeconds <= 0 || IsHardCrowdControlStatus(statusId)) return baseDurationSeconds;
        if (!ShouldScaleDuration(effectType)) return baseDurationSeconds;

        var increase = Math.Min(MaxDurationGrowth, DurationGrowthPerAscensionTier * Math.Max(0, ascensionTier));
        return baseDurationSeconds * (1 + increase);
    }

    public static double GetSummonPowerMultiplier(int ascensionTier) =>
        1 + SummonPowerGrowthPerAscensionTier * Math.Max(0, ascensionTier);

    public static double GetSummonHealthMultiplier(int ascensionTier) =>
        1 + SummonHealthGrowthPerAscensionTier * Math.Max(0, ascensionTier);

    private static double GetTierValueGrowth(string? effectType) =>
        effectType switch
        {
            AbilityEffectType.Damage => DamageValueGrowthPerAscensionTier,
            AbilityEffectType.ReflectDamage => DamageValueGrowthPerAscensionTier,
            AbilityEffectType.Heal => HealingValueGrowthPerAscensionTier,
            AbilityEffectType.RestoreResource => HealingValueGrowthPerAscensionTier,
            AbilityEffectType.GrantBarrier => BarrierValueGrowthPerAscensionTier,
            AbilityEffectType.AbsorbDamage => BarrierValueGrowthPerAscensionTier,
            AbilityEffectType.ModifyAttribute => AttributeValueGrowthPerAscensionTier,
            AbilityEffectType.Taunt => AttributeValueGrowthPerAscensionTier,
            AbilityEffectType.ModifyStatusEffect => StatusStackValueGrowthPerAscensionTier,
            _ => 0
        };

    private static bool ShouldScaleDuration(string? effectType) =>
        effectType is AbilityEffectType.ApplyStatus or AbilityEffectType.ModifyAttribute or AbilityEffectType.ModifyStatusEffect;

    private static bool IsHardCrowdControlStatus(string? statusId) =>
        statusId is not null
        && HardCrowdControlStatuses.Contains(statusId);

    private static readonly HashSet<string> HardCrowdControlStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Asleep",
        "Charmed",
        "Confused",
        "Frozen",
        "Petrified",
        "Rooted",
        "Silenced",
        "Stunned"
    };
}

public sealed record EssenceAscensionCost(string ItemId, int Amount);
