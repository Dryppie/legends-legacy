namespace Domain.Models.Nobility;

public enum SignetOrigin { AlphaGrant, Purchase, Replacement, GameplayReward }

public sealed class SignetIssuance
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public string? ActorSubject { get; set; }
    public SignetOrigin Origin { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public bool Refunded { get; set; }
}
