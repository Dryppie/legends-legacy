namespace Domain.Models.Prophecies;

public sealed class DailyProphecyRerollState
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid CharacterId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public int RerollsUsed { get; set; }
    public long FateEchoSpent { get; set; }
    public string ShownDefinitionIdsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}
