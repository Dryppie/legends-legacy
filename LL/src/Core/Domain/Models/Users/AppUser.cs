using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Users;
public sealed class AppUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    [NotMapped]
    public Guid? CharacterId { get; set; }
    public bool IsGuest { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // factory helpers --------------------------------------------------------

    public static AppUser Guest() => new();
    public static AppUser Register(string username, string email, string hash) =>
        new()
        {
            Username = username,
            Email = email,
            PasswordHash = hash,
            IsGuest = false
        };
    public void ConvertGuestToAccount(string username, string email, string hash)
    {
        Username = username;
        Email = email;
        PasswordHash = hash;
        UpdatedUtc = DateTime.UtcNow;
        IsGuest = false;
    }
}