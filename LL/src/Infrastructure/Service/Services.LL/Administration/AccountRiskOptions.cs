using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountRiskOptions
{
    public const string SectionName = "LiveOps:AccountRisk";

    public bool Enabled { get; set; } = true;
    public int EvaluationVersion { get; set; } = 5;
    public int EvaluationIntervalMinutes { get; set; } = 30;
    public int LookbackDays { get; set; } = 90;
    public int CandidateLimit { get; set; } = 2_000;
    public int MaximumTransfersPerEvaluation { get; set; } = 100_000;
    public int HistoryMinimumScoreChange { get; set; } = 5;
    public int ModerateScore { get; set; } = 25;
    public int HighScore { get; set; } = 50;
    public int CriticalScore { get; set; } = 75;
    public int MinimumTransferCount { get; set; } = 1;
    public int MinimumCounterpartyCount { get; set; } = 1;
    public decimal ConcentrationThreshold { get; set; } = 0.70m;
    public decimal RelationshipImbalanceThreshold { get; set; } = 0.85m;
    public int YoungAccountDays { get; set; } = 14;
    public int YoungAccountMaximumLevel { get; set; } = 20;
    public decimal FeederTargetShareThreshold { get; set; } = 0.80m;
    public int CircularWindowHours { get; set; } = 48;
    public decimal CircularValueSimilarity { get; set; } = 0.50m;
    public int FlowCategoryCap { get; set; } = 55;
    public int CoordinationCategoryCap { get; set; } = 25;

    public AccountRiskPolicy ToPolicy() => new(
        ModerateScore,
        HighScore,
        CriticalScore,
        MinimumTransferCount,
        MinimumCounterpartyCount,
        ConcentrationThreshold,
        RelationshipImbalanceThreshold,
        YoungAccountDays,
        YoungAccountMaximumLevel,
        FeederTargetShareThreshold,
        CircularWindowHours,
        CircularValueSimilarity,
        FlowCategoryCap,
        CoordinationCategoryCap);
}
