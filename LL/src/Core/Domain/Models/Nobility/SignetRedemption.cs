namespace Domain.Models.Nobility;

public sealed class SignetRedemption
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public Guid[] UnitIds { get; set; } = [];
    public Guid MembershipVersion { get; set; }
    public DateTimeOffset RedeemedAt { get; set; }
    public DateTimeOffset? PreviousExpiry { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
