namespace Domain.Models.Administration;

public enum AccountRestrictionType
{
    Ban
}

public sealed class AccountRestriction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public AccountRestrictionType RestrictionType { get; set; } = AccountRestrictionType.Ban;
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string CreatedBySubject { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? RevokedBySubject { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && (!ExpiresAt.HasValue || ExpiresAt.Value > now);

    public void Revoke(string actorSubject, string reason, DateTimeOffset revokedAt)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedBySubject = actorSubject.Trim();
        RevocationReason = reason.Trim();
        RevokedAt = revokedAt;
    }
}
