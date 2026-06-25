namespace Domain.Models.Prophecies;

public sealed class WeeklyRevelationProgress
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid CharacterId { get; set; }

    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }

    public int PropheticFavor { get; set; }

    public bool Milestone3Claimed { get; set; }
    public bool Milestone5Claimed { get; set; }
    public bool Milestone7Claimed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
