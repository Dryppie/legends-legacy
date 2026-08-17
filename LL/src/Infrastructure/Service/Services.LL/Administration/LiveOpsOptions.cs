namespace Services.LL.Administration;

public sealed class LiveOpsOptions
{
    public const string SectionName = "LiveOps";

    public int MaximumGrantQuantity { get; set; } = 100_000;
}
