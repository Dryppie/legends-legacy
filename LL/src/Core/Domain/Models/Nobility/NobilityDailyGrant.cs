namespace Domain.Models.Nobility;

public sealed class NobilityDailyGrant
{
    public Guid AccountId { get; set; }
    public DateOnly GameDate { get; set; }
    public Guid CharacterId { get; set; }
    public int SigilFragments { get; set; } = 2;
    public int Soulstones { get; set; } = 10;
    public int PolicyVersion { get; set; } = NobilityBenefits.PolicyVersion;
    public DateTimeOffset AppliedAt { get; set; }
}
