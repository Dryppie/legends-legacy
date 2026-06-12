namespace Domain.Models.Essences.Definitions;

public sealed class EssenceProgressionTemplate
{
    public string Id { get; set; } = string.Empty;
    public int BaseXpPerLevel { get; set; } = 100;
    public double XpGrowth { get; set; } = 1.18;
    public double ActiveScalingMultiplier { get; set; } = 1;
    public double PassiveScalingMultiplier { get; set; } = 1;

    public int GetXpRequiredForLevel(int level)
    {
        if (level >= 40) return 0;
        return (int)Math.Ceiling(BaseXpPerLevel * Math.Pow(XpGrowth, Math.Max(0, level - 1)));
    }
}
