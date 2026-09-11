namespace Domain.Models.Nobility;

// Historical receipts only. Nobility no longer grants daily resources.
public sealed class NobilityDailyGrant
{
    public Guid AccountId { get; set; }
    public DateOnly GameDate { get; set; }
    public Guid CharacterId { get; set; }
    public int SigilFragments { get; set; }
    public int Soulstones { get; set; }
    public int PolicyVersion { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
}
