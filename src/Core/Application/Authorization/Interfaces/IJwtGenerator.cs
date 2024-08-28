using Common.Authorization.Security;
using Domain.Models.Users;

namespace Application.Authorization.Interfaces;
public interface IJwtGenerator
{
    /// <summary>
    /// Generates a new access token and refresh token on login
    /// </summary>
    /// <param name="authInfo"></param>
    /// <returns>Tokens</returns>
    Tokens GenerateTokens(AuthInfo authInfo);

    /// <summary>
    /// Validates a jwt access token
    /// </summary>
    /// <param name="token"></param>
    /// <returns>Validity bool</returns>
    bool ValidateAccessToken(string token);

    /// <summary>
    /// <para>
    /// Exchanges a refresh token for a new access token and refresh token
    /// The new refresh token will use a sliding window for its new expiration time
    /// </para>
    /// <para>If isRefreshFromWeb is true, extra logic for who is allowed to refresh tokens from the Web is applied</para>
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="isRefreshFromWeb"></param>
    /// <returns>Tokens</returns>
    Tokens RefreshTokens(string refreshToken, bool isRefreshFromWeb = false);

    string CreateAccessToken(AppUser appUser);
    string CreateRefreshToken(AppUser appUser);
}