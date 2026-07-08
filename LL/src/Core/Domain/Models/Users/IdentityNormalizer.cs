namespace Domain.Models.Users;

public static class IdentityNormalizer
{
    public static string NormalizeRequired(string value) =>
        value.Trim().ToUpperInvariant();

    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(value);
    }
}
