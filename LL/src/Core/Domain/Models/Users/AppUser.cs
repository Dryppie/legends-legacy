using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Users;
public sealed class AppUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    [NotMapped]
    public Guid? CharacterId { get; set; }
    public bool IsGuest { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public bool IsNameEdited { get; set; }

    // factory helpers --------------------------------------------------------

    public static AppUser Guest() => new();
    public static AppUser Register(string username, string email, string hash) =>
        new()
        {
            Username = username.Trim(),
            NormalizedUsername = IdentityNormalizer.NormalizeRequired(username),
            Email = email.Trim(),
            NormalizedEmail = IdentityNormalizer.NormalizeRequired(email),
            PasswordHash = hash,
            IsGuest = false
        };
    public void ConvertGuestToAccount(string username, string email, string hash)
    {
        Username = username.Trim();
        NormalizedUsername = IdentityNormalizer.NormalizeRequired(username);
        Email = email.Trim();
        NormalizedEmail = IdentityNormalizer.NormalizeRequired(email);
        PasswordHash = hash;
        UpdatedUtc = DateTime.UtcNow;
        IsGuest = false;
    }

    public void ConvertGuestToExternalAccount(string username, string email)
    {
        Username = username.Trim();
        NormalizedUsername = IdentityNormalizer.NormalizeRequired(username);
        Email = email.Trim();
        NormalizedEmail = IdentityNormalizer.NormalizeRequired(email);
        EmailConfirmed = true;
        UpdatedUtc = DateTime.UtcNow;
        IsGuest = false;
    }

    public void ConfirmExternalEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Email = email.Trim();
            NormalizedEmail = IdentityNormalizer.NormalizeRequired(email);
        }

        EmailConfirmed = true;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void NormalizeIdentityFields()
    {
        Username = Username.Trim();
        NormalizedUsername = string.IsNullOrWhiteSpace(Username)
            ? string.Empty
            : IdentityNormalizer.NormalizeRequired(Username);

        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        NormalizedEmail = IdentityNormalizer.NormalizeOptional(Email);
    }
}
