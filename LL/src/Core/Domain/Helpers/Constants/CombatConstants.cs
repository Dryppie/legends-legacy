namespace Domain.Helpers.Constants;
public static class CombatConstants
{
    // Hit
    public const float BASE_HIT_CHANCE = 98f;
    public const float MIN_HIT_CHANCE = 10f;
    public const float MAX_HIT_CHANCE = 100f;

    // Dodge
    public const float BASE_DODGE_CHANCE = 5f;
    public const float MIN_DODGE_CHANCE = 1f;
    public const float MAX_DODGE_CHANCE = 70f;

    // Block
    public const float BASE_BLOCK_VALUE = 0.053f;
    public const float MAX_BLOCK_CHANCE = 70f;
    public const float BLOCK_DAMAGE_DECREASE = 0.4f;

    // Parry
    public const float BASE_PARRY_VALUE = 0.04f;
    public const float MAX_PARRY_CHANCE = 20f;
    public const float PARRY_DAMAGE_DECREASE = 1f;

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
        Random rand = new();
        return rand.Next(floorMin, ceilMax + 1);
    }
}