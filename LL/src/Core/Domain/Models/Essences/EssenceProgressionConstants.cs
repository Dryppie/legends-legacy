namespace Domain.Models.Essences;

public static class EssenceProgressionConstants
{
    public const int BaseXpPerLevel = 100;
    public const double XpGrowth = 1.18;

    public static int GetXpRequiredForLevel(int level)
    {
        if (level >= 40) return 0;
        return (int)Math.Ceiling(BaseXpPerLevel * Math.Pow(XpGrowth, Math.Max(0, level - 1)));
    }
}
