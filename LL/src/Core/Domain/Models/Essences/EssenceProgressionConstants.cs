namespace Domain.Models.Essences;

public static class EssenceProgressionConstants
{
    public const int BaseXpPerLevel = 100;
    public const double XpGrowth = 1.18;
    public const double AbilityValueGrowthPerLevel = 0.04;
    public const double AttributeBonusGrowthPerLevel = 0.04;

    public static int GetXpRequiredForLevel(int level)
    {
        if (level >= 40) return 0;
        return (int)Math.Ceiling(BaseXpPerLevel * Math.Pow(XpGrowth, Math.Max(0, level - 1)));
    }

    public static double ScaleAbilityValue(double baseValue, int level)
    {
        var multiplier = 1 + AbilityValueGrowthPerLevel * Math.Max(0, level - 1);
        return baseValue * multiplier;
    }

    public static double ScaleAttributeBonus(double baseValue, int level)
    {
        var multiplier = 1 + AttributeBonusGrowthPerLevel * Math.Max(0, level - 1);
        return baseValue * multiplier;
    }
}
