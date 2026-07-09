using System.Net.Mail;

namespace Application.UseCases.Users;

public sealed record ValidatedAuthRegistration(string CharacterName, string Email, string Password);

public static class AuthInputValidator
{
    private const int MaxCharacterNameLength = 26;
    private const int MinPasswordLength = 8;

    public static bool TryValidateRegistration(
        string? characterName,
        string? email,
        string? password,
        out ValidatedAuthRegistration validated,
        out string error)
    {
        validated = new ValidatedAuthRegistration(string.Empty, string.Empty, string.Empty);

        if (!TryValidateName(characterName, "Character name", out var validCharacterName, out error))
        {
            return false;
        }

        if (!TryValidateEmail(email, out var validEmail, out error))
        {
            return false;
        }

        if (!TryValidatePassword(password, out var validPassword, out error))
        {
            return false;
        }

        validated = new ValidatedAuthRegistration(validCharacterName, validEmail, validPassword);
        return true;
    }

    public static bool TryValidateName(string? value, string label, out string name, out string error)
    {
        name = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{label} is required.";
            return false;
        }

        name = value.Trim();
        if (name.Length > MaxCharacterNameLength)
        {
            error = $"{label} is too long.";
            return false;
        }

        return true;
    }

    private static bool TryValidateEmail(string? value, out string email, out string error)
    {
        email = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Email is required.";
            return false;
        }

        email = value.Trim();
        if (email.Contains(' '))
        {
            error = "Email format is invalid.";
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                error = "Email format is invalid.";
                return false;
            }
        }
        catch (FormatException)
        {
            error = "Email format is invalid.";
            return false;
        }

        return true;
    }

    private static bool TryValidatePassword(string? value, out string password, out string error)
    {
        password = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Password is required.";
            return false;
        }

        password = value;
        if (password.Length < MinPasswordLength)
        {
            error = $"Password must be at least {MinPasswordLength} characters.";
            return false;
        }

        return true;
    }
}
