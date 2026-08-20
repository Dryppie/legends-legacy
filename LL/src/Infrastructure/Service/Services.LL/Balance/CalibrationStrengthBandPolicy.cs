using Application.Interfaces.Services.LL.Balance;

namespace Services.LL.Balance;

/// <summary>
/// Converts a checkpoint's authored equipment rung into realistic adjacent
/// progression states. It deliberately moves through real rarity/tempering
/// rungs rather than multiplying aggregate character stats.
/// </summary>
public static class CalibrationStrengthBandPolicy
{
    public static int ResolveRungIndex(
        int expectedRungIndex,
        int maximumAvailableRungIndex,
        CalibrationStrengthBand strength)
    {
        if (expectedRungIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedRungIndex));
        if (maximumAvailableRungIndex < expectedRungIndex)
            throw new ArgumentOutOfRangeException(nameof(maximumAvailableRungIndex));

        var offset = strength switch
        {
            CalibrationStrengthBand.Undergeared => -1,
            CalibrationStrengthBand.Expected => 0,
            CalibrationStrengthBand.WellGeared => 1,
            CalibrationStrengthBand.Optimized => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
        };
        return Math.Clamp(
            expectedRungIndex + offset,
            0,
            maximumAvailableRungIndex);
    }
}
