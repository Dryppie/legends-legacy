namespace Services.LL.Combat.Profiles;

public static class WorldTowerProfileTargetContract
{
    public const int Version = 4;
    public const int SelectionConfirmationSampleCount = 100;
    public const string CertificationSeedManifestId = "world-tower-certification-v1";
    public const double MinimumWinRate = 0.05d;
    public const double MaximumWinRate = 0.20d;

    public static bool Contains(double winRate) =>
        double.IsFinite(winRate)
        && winRate > MinimumWinRate
        && winRate < MaximumWinRate;

    public static bool IsBelowMaximum(double winRate) =>
        double.IsFinite(winRate)
        && winRate < MaximumWinRate;
}
