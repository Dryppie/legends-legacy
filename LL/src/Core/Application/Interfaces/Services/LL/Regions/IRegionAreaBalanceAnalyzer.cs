namespace Application.Interfaces.Services.LL.Regions;

public interface IRegionAreaBalanceAnalyzer
{
    Task<RegionAreaBalanceReport> AnalyzeAsync(
        RegionAreaBalanceRequest request,
        CancellationToken cancellationToken);
}

public sealed record RegionAreaBalanceRequest(
    string RegionKey,
    int EncountersPerProfile,
    int RandomSeed);

public sealed record RegionAreaBalanceReport(
    string RegionKey,
    int BalanceVersion,
    int TargetWinRateBasisPoints,
    int EncountersPerProfile,
    bool IsSmooth,
    bool IsWithinTolerance,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<RegionAreaBalanceResult> Areas);

public sealed record RegionAreaBalanceResult(
    string AreaId,
    string AreaName,
    int GlobalStep,
    int LevelRequirement,
    string BuildId,
    string Status,
    double AverageWinRate,
    double LowestProfileWinRate,
    decimal EffectiveExperiencePerHour,
    decimal EffectiveCindersPerHour,
    CreatureScalingProfile Scaling,
    IReadOnlyList<RegionAreaProfileBalanceResult> Profiles);

public sealed record RegionAreaProfileBalanceResult(
    string Profile,
    double WinRate,
    double AverageCombatTicks,
    double P95DamageTaken);
