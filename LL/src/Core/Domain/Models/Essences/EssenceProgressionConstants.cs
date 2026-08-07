namespace Domain.Models.Essences;

public static class EssenceProgressionConstants
{
    public const int BaseXpPerLevel = 132_860;
    public const double XpGrowth = 1.02;
    public const int MaxEssenceLevel = 100;
    public const int MaxAscensionTier = 3;
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
    public const int AscensionTierOneRequiredLevel = 10;
    public const int AscensionTierTwoRequiredLevel = 30;
    public const int AscensionTierThreeRequiredLevel = 60;
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
        2 => 60,
        _ => MaxEssenceLevel
    };

    public static EssenceAscensionRequirement GetAscensionRequirement(int nextAscensionTier) =>
        nextAscensionTier switch
        {
            1 => new(AscensionTierOneRequiredLevel),
            2 => new(AscensionTierTwoRequiredLevel),
            3 => new(AscensionTierThreeRequiredLevel),
            _ => throw new ArgumentOutOfRangeException(nameof(nextAscensionTier), "Ascension tier must be between 1 and 3.")
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

    public static double ScaleAbilityValue(double baseValue, int ascensionTier = 0, string? effectType = null)
    {
        var multiplier = 1 + GetTierValueGrowth(effectType) * Math.Max(0, ascensionTier);
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
            "Damage" => DamageValueGrowthPerAscensionTier,
            "ReflectDamage" => DamageValueGrowthPerAscensionTier,
            "Heal" => HealingValueGrowthPerAscensionTier,
            "RestoreResource" => HealingValueGrowthPerAscensionTier,
            "GrantBarrier" => BarrierValueGrowthPerAscensionTier,
            "AbsorbDamage" => BarrierValueGrowthPerAscensionTier,
            "ModifyAttribute" => AttributeValueGrowthPerAscensionTier,
            "Taunt" => AttributeValueGrowthPerAscensionTier,
            "ModifyStatusStacks" => StatusStackValueGrowthPerAscensionTier,
            "ModifyStatusEffect" => StatusStackValueGrowthPerAscensionTier,
            "ModifyThreat" => AttributeValueGrowthPerAscensionTier,
            "ModifyRegenerationRate" => AttributeValueGrowthPerAscensionTier,
            "ModifyRegenerationInterval" => AttributeValueGrowthPerAscensionTier,
            "ModifyHealingReceived" => AttributeValueGrowthPerAscensionTier,
            "ModifyDamageDealt" => AttributeValueGrowthPerAscensionTier,
            "ModifyDamageTaken" => AttributeValueGrowthPerAscensionTier,
            "ModifyDamageTakenFromCondition" => AttributeValueGrowthPerAscensionTier,
            "ModifyNextBasicAttackDamage" => DamageValueGrowthPerAscensionTier,
            "ModifyNextBasicAttackArmorPenetration" => AttributeValueGrowthPerAscensionTier,
            _ => 0
        };

    private static bool ShouldScaleDuration(string? effectType) =>
        effectType is "ApplyStatus" or "ModifyAttribute" or "ModifyStatusStacks" or "ModifyStatusEffect";

    private static bool IsHardCrowdControlStatus(string? statusId) =>
        statusId is not null
        && HardCrowdControlStatuses.Contains(NormalizeStatusKey(statusId));

    private static string NormalizeStatusKey(string statusId)
    {
        var normalized = statusId.StartsWith("status.", StringComparison.OrdinalIgnoreCase)
            ? statusId["status.".Length..]
            : statusId;

        return normalized.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> HardCrowdControlStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Asleep",
        "Charmed",
        "Confused",
        "Frozen",
        "Petrified",
        "Rooted",
        "Silenced",
        "Stunned",
        "Freeze",
        "Stun"
    };
}

public sealed record EssenceAscensionCost(string ItemId, int Amount);
public sealed record EssenceAscensionRequirement(int RequiredLevel);
