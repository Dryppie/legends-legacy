namespace Domain.Models.Nobility;

public enum SignetState { Available, Listed, Redeemed, Revoked }

public sealed class SignetUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssuanceId { get; set; }
    public int Ordinal { get; set; }
    public Guid OwnerCharacterId { get; set; }
    public SignetState State { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? RedemptionId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset IssuedAt { get; set; }
}
