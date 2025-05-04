namespace Domain.Models.Users;
public sealed class RefreshToken
{
    public long Id { get; init; }
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = default!;
    public DateTime ExpiresUtc { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? RevokedUtc { get; set; }
    public string? ReplacedBy { get; set; }
    public bool IsActive => RevokedUtc is null && DateTime.UtcNow <= ExpiresUtc;
}