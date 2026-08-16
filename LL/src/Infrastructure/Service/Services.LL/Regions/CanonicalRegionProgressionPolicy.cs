namespace Services.LL.Regions;

/// <summary>
/// Defines open-ended formula progression used by balance diagnostics.
/// Regions that eventually use a different level cadence must introduce a new
/// policy version instead of silently changing existing calibration anchors.
/// </summary>
public static class CanonicalRegionProgressionPolicy
{
    public const int Version = 2;
    public const int AuthoredRegionCount = 10;
    public const int RegionCount = AuthoredRegionCount;
    public const int AreasPerRegion = 10;
    public const int LevelsPerArea = 5;

    public static int GetEquipmentTier(int regionNumber) =>
        ValidateRegionNumber(regionNumber);

    public static int GetEndingCharacterLevel(int regionNumber) =>
        checked(ValidateRegionNumber(regionNumber) * AreasPerRegion * LevelsPerArea -
                LevelsPerArea);

    private static int ValidateRegionNumber(int regionNumber)
    {
        if (regionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(regionNumber),
                regionNumber,
                "Canonical region numbers must be positive.");
        }

        return regionNumber;
    }
}
