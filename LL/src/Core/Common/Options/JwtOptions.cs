namespace Common.Options;
public sealed class JwtOptions
{
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public string SigningKey { get; init; } = default!;
    public int AccessMinutes { get; init; } = 30;
    public int RefreshDays { get; init; } = 30;
}