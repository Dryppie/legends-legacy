namespace Domain.Models.Users;
public sealed class ExternalLogin
{
    public long Id { get; init; }
    public Guid UserId { get; init; }
    public AppUser User { get; init; } = default!;
    public AuthProvider Provider { get; init; }
    public string ProviderUserId { get; init; } = default!;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresUtc { get; set; }
}