namespace Domain.Models.Nobility;

public sealed class NobilityCoverage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int CalendarMonths { get; set; }
    public int PolicyVersion { get; set; } = NobilityBenefits.PolicyVersion;
}
