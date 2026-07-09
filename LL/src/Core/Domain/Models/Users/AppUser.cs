using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Users;
public sealed class AppUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
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
    public static AppUser Register(string accountLabel, string email, string hash) =>
        new()
        {
            Username = accountLabel.Trim(),
            Email = email.Trim(),
            NormalizedEmail = IdentityNormalizer.NormalizeRequired(email),
            PasswordHash = hash,
            IsGuest = false
        };

    public void ConvertGuestToAccount(string email, string hash)
    {
        Email = email.Trim();
        NormalizedEmail = IdentityNormalizer.NormalizeRequired(email);
        PasswordHash = hash;
        UpdatedUtc = DateTime.UtcNow;
        IsGuest = false;
    }

    public void ConvertGuestToExternalAccount(string email)
    {
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
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        NormalizedEmail = IdentityNormalizer.NormalizeOptional(Email);
    }
}
