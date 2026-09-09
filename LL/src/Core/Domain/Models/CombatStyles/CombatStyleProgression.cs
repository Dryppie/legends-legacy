namespace Domain.Models.CombatStyles;

public sealed record CombatStyleXpGrantResult(long XpGained, int LevelsGained, int Level, long CurrentXp, bool ReachedCap);

public static class CombatStyleProgression
{
    public const int MaximumLevel = 10;
    public const int OpeningTechniqueLevel = 7;
    public const int UpgradeMasteryLevel = 9;
    public static int UpgradeSlots(int level) => level >= 8 ? 2 : level >= 5 ? 1 : 0;
    public static long XpRequired(int level, IReadOnlyList<long> requirements) =>
        level >= MaximumLevel ? 0 : requirements[level];

    public static CombatStyleXpGrantResult Grant(CharacterCombatStyle style, long eligibleBaseXp, IReadOnlyList<long> requirements)
    {
        if (eligibleBaseXp < 0) throw new ArgumentOutOfRangeException(nameof(eligibleBaseXp));
        if (requirements.Count != MaximumLevel || requirements.Any(x => x <= 0))
            throw new ArgumentException("Combat Styles require ten positive XP requirements.", nameof(requirements));
        if (style.Level is < 0 or > MaximumLevel || style.CurrentXp < 0
            || (style.Level < MaximumLevel && style.CurrentXp >= requirements[style.Level]))
            throw new InvalidOperationException("Invalid saved Combat Style progression.");
        var originalLevel = style.Level;
        var remaining = eligibleBaseXp;
        while (style.Level < MaximumLevel && remaining > 0)
        {
            var needed = requirements[style.Level] - style.CurrentXp;
            var gained = Math.Min(remaining, needed);
            style.CurrentXp += gained;
            remaining -= gained;
            if (style.CurrentXp == requirements[style.Level])
            {
                style.Level++;
                style.CurrentXp = 0;
            }
        }
        if (style.Level == MaximumLevel) style.CurrentXp = 0;
        return new(eligibleBaseXp - remaining, style.Level - originalLevel, style.Level, style.CurrentXp, style.Level == MaximumLevel);
    }
}
