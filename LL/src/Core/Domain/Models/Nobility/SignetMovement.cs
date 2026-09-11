namespace Domain.Models.Nobility;

public enum SignetMovementKind { Issued, Reserved, Released, Traded, Redeemed, Revoked }

public sealed class SignetMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitId { get; set; }
    public Guid OperationId { get; set; }
    public SignetMovementKind Kind { get; set; }
    public Guid? FromCharacterId { get; set; }
    public Guid? ToCharacterId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
