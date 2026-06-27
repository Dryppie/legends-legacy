namespace Domain.Models.Guilds.Missions;

public class GuildContributionLedger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guid CharacterId { get; set; }
    public GuildContributionSource Source { get; set; }
    public GuildContributionMetric Metric { get; set; }
    public long Amount { get; set; }
    public string? ContextId { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
