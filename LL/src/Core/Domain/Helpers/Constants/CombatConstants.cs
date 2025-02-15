namespace Domain.Helpers.Constants;
public static class CombatConstants
{
    // Hit
    public const float BaseHitChance = 98f;
    public const float MinHitChance = 10f;
    public const float MaxHitChance = 100f;

    // Dodge
    public const float BaseDodgeChance = 5f;
    public const float MinDodgeChance = 1f;
    public const float MaxDodgeChance = 70f;

    // TODO: Add a magnitude range to effects. Some might have a range of 0.9f (90%),
    // such that they perhaps deal (15 +/- 90%) = 1-29 damage. Others might just be +/- 20% (Default range)
    private const float MAGNITUDE_RANGE = 0.2f;

    /// <summary>
    /// Returns a random integer between the min and max of value 
    /// where min/max is determined by +/- MAGNITUDE_RANGE.
    /// </summary>
    /// <param name="value">The base value for which the +/- range is calculated.</param>
    /// <returns>A random integer within the computed range.</returns>
    public static int GetRandomValue(double value)
    {
        double min = value * (1.0 - MAGNITUDE_RANGE);
        double max = value * (1.0 + MAGNITUDE_RANGE);

        // Round down and up respectively:
        int floorMin = (int)Math.Floor(min);
        int ceilMax = (int)Math.Ceiling(max);

        // Use Random.Next(minValue, maxValueExclusive).
        // So we add 1 to make it inclusive of the upper bound.
        Random rand = new Random();
        return rand.Next(floorMin, ceilMax + 1);
    }
}