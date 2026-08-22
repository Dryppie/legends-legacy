using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountTemporalCorrelationOptions
{
    public const string SectionName = "LiveOps:AccountTemporalCorrelation";

    public int AnalysisVersion { get; set; } = 1;
    public int DefaultWindowDays { get; set; } = 90;
    public int MaximumWindowDays { get; set; } = 90;
    public int RelatedAccountLimit { get; set; } = 20;
    public int MaximumTokenRows { get; set; } = 20_000;
    public int MaximumTransferRows { get; set; } = 2_000;
    public int MinimumActiveDays { get; set; } = 7;
    public int NearStartWindowMinutes { get; set; } = 15;
    public int StrongNearStartWindowMinutes { get; set; } = 5;
    public int TransferAdjacentWindowMinutes { get; set; } = 15;
    public int MinimumRepeatedMatchDays { get; set; } = 3;
    public int ModerateMinimumMatches { get; set; } = 4;
    public decimal ModerateMinimumLift { get; set; } = 2m;
    public int HighMinimumRepeatedMatchDays { get; set; } = 5;
    public int HighMinimumMatches { get; set; } = 6;
    public decimal HighMinimumLift { get; set; } = 3m;
    public int HighMinimumTransferAdjacentMatches { get; set; } = 2;
    public int MaximumDisplayedMatches { get; set; } = 20;

    public AccountTemporalCorrelationPolicy ToPolicy() => new(
        AnalysisVersion,
        MinimumActiveDays,
        NearStartWindowMinutes,
        StrongNearStartWindowMinutes,
        TransferAdjacentWindowMinutes,
        MinimumRepeatedMatchDays,
        ModerateMinimumMatches,
        ModerateMinimumLift,
        HighMinimumRepeatedMatchDays,
        HighMinimumMatches,
        HighMinimumLift,
        HighMinimumTransferAdjacentMatches,
        MaximumDisplayedMatches);
}
