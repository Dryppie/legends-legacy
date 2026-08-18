using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountRiskOptions
{
    public const string SectionName = "LiveOps:AccountRisk";

    public bool Enabled { get; set; } = true;
    public int EvaluationVersion { get; set; } = 8;
    public int EvaluationIntervalMinutes { get; set; } = 30;
    public int LookbackDays { get; set; } = 90;
    public int CandidateLimit { get; set; } = 2_000;
    public int MaximumTransfersPerEvaluation { get; set; } = 100_000;
    public int HistoryMinimumScoreChange { get; set; } = 5;
    public int ModerateScore { get; set; } = 25;
    public int HighScore { get; set; } = 50;
    public int CriticalScore { get; set; } = 75;
    public int MinimumTransferCount { get; set; } = 2;
    public int MinimumCounterpartyCount { get; set; } = 2;
    public long MinimumRelationshipCinders { get; set; } = 10_000;
    public int MinimumItemTransferCount { get; set; } = 2;
    public int MinimumItemFunnelTransferCount { get; set; } = 20;
    public int MinimumItemFunnelCounterpartyCount { get; set; } = 2;
    public int ItemFunnelFullScaleTransferCount { get; set; } = 150;
    public decimal ItemFunnelIncomingShareThreshold { get; set; } = 0.85m;
    public int MinimumConsolidatedItemAssetCount { get; set; } = 2;
    public long MinimumConsolidatedItemQuantity { get; set; } = 50;
    public int MinimumConsolidatedItemTransferCount { get; set; } = 10;
    public decimal ConsolidatedItemIncomingShareThreshold { get; set; } = 0.80m;
    public int MinimumYoungItemSourceTransferCount { get; set; } = 20;
    public int MinimumYoungItemSourceCounterpartyCount { get; set; } = 2;
    public int MinimumYoungItemCoordinationTransferCount { get; set; } = 50;
    public int MinimumYoungItemCoordinationCounterpartyCount { get; set; } = 4;
    public int MinimumMixedDirectionItemTransferCount { get; set; } = 10;
    public int ItemTransferSessionWindowMinutes { get; set; } = 5;
    public int MinimumItemCoordinationSessionCount { get; set; } = 20;
    public decimal ItemCoordinationDominantSessionShareThreshold { get; set; } = 0.70m;
    public long MinimumFeederCinders { get; set; } = 20_000;
    public long MinimumYoungAccountOutflowCinders { get; set; } = 10_000;
    public long MinimumCircularTransferCinders { get; set; } = 10_000;
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
        MinimumRelationshipCinders,
        MinimumItemTransferCount,
        MinimumItemFunnelTransferCount,
        MinimumItemFunnelCounterpartyCount,
        ItemFunnelFullScaleTransferCount,
        ItemFunnelIncomingShareThreshold,
        MinimumConsolidatedItemAssetCount,
        MinimumConsolidatedItemQuantity,
        MinimumConsolidatedItemTransferCount,
        ConsolidatedItemIncomingShareThreshold,
        MinimumYoungItemSourceTransferCount,
        MinimumYoungItemSourceCounterpartyCount,
        MinimumYoungItemCoordinationTransferCount,
        MinimumYoungItemCoordinationCounterpartyCount,
        MinimumMixedDirectionItemTransferCount,
        ItemTransferSessionWindowMinutes,
        MinimumItemCoordinationSessionCount,
        ItemCoordinationDominantSessionShareThreshold,
        MinimumFeederCinders,
        MinimumYoungAccountOutflowCinders,
        MinimumCircularTransferCinders,
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
