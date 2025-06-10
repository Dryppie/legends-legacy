namespace Common.Authorization.Security;
public sealed record Tokens(string AccessToken, string RefreshToken, long AccessExpiresAt);