namespace Services.LL.Administration;

public sealed class LiveOpsOptions
{
    public const string SectionName = "LiveOps";

    public int MaximumGrantQuantity { get; set; } = 100_000;
    public int LargeGrantAuditThreshold { get; set; } = 100;
    public int PreviewLifetimeSeconds { get; set; } = 300;
    public int SupportSnapshotSectionTimeoutSeconds { get; set; } = 3;
}
