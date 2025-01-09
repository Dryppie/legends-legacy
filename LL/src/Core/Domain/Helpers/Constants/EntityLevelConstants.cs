namespace Domain.Helpers.Constants;
public static class EntityLevelConstants
{
    private const float XP_RATE = 0.5f;
    private const float BASE_XP = 100;
    private const int NTH = 4;
    private static float HUNDREDTH(int level) => MathF.Ceiling(level / 100f);

    public static float XP_REQUIRED(int level) => MathF.Floor(XP_RATE * MathF.Pow(HUNDREDTH(level), 2) * XP_RATE * MathF.Sqrt(MathF.Pow(level, NTH)) + BASE_XP + MathF.Pow(level, 2) * (5 - HUNDREDTH(level)));
}