namespace Domain.Models.Dungeons.Mastery;

public static class DungeonMasteryProgression
{
    // Cumulative XP: ten times the original curve, with unchanged XP per clear.
    private static readonly int[] LevelThresholds =
        [1000, 2500, 5000, 9000, 14000, 21000, 30000, 42000, 56000, 75000];

    public static int CalculateLevel(long experience) =>
        LevelThresholds.Count(threshold => experience >= threshold);

    public static int? GetExperienceRequiredForNextLevel(int level) =>
        level >= DungeonMasteryBenefits.MaxLevel ? null : LevelThresholds[Math.Max(0, level)];
}
