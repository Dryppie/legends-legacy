namespace Domain.Helpers.Constants;
public static class ArmorDamageReductionConstants
{
    private const int REAL_K = 400;
    private const float K_SCALE = 1.5f;

    private const float LEVEL_MODIFIER_SCALE = 0.05f;
    private const int MIN_LEVEL_DIFFERENCE = -100;
    private const int MAX_LEVEL_DIFFERENCE = 100;
    private const double LEVEL_INTERVAL = 10.0;

    public static float CalculateEffectiveDefense(double defense, int levelDifference)
    {
        var levelModifier = CalculateLevelModifier(levelDifference);
        var k = CalculateK(levelModifier);

        return (float)Math.Round((1 - Math.Exp(-(defense / k))), 2);
    }

    private static double CalculateK(double levelModifier) => REAL_K * (1 - (levelModifier - 1) * K_SCALE);

    private static double CalculateLevelModifier(int levelDifference)
    {
        int cappedDifference = Math.Clamp(levelDifference, MIN_LEVEL_DIFFERENCE, MAX_LEVEL_DIFFERENCE);
        double truncatedValue = Math.Truncate(cappedDifference / LEVEL_INTERVAL);
        var levelModifier = 1 + (LEVEL_MODIFIER_SCALE * truncatedValue);
        return Math.Round(levelModifier, 2);
    }
}