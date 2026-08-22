namespace Services.LL.Administration;

public sealed class LiveOpsOptions
{
    public const string SectionName = "LiveOps";

    public int MaximumGrantQuantity { get; set; } = 100_000;
    public int LargeGrantAuditThreshold { get; set; } = 100;
    public int PreviewLifetimeSeconds { get; set; } = 300;
    public int SupportSnapshotSectionTimeoutSeconds { get; set; } = 3;
    public int TransferConversationLookbackDays { get; set; } = 30;
    public int TransferConversationAfterHours { get; set; } = 2;
    public int TransferConversationImmediateBeforeHours { get; set; } = 24;
    public int TransferConversationRelationshipDays { get; set; } = 90;
    public int MaximumTransferConversationCorrelationRows { get; set; } = 500;
    public int UncommunicativeMinimumTransferCount { get; set; } = 3;
    public long UncommunicativeMinimumCinders { get; set; } = 10_000;
    public int UncommunicativeMinimumItemTransferCount { get; set; } = 3;
}
