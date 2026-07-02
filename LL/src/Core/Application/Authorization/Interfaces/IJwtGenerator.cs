using Common.Authorization.Security;
using Domain.Models.Entities.Characters;
using Domain.Models.Users;

namespace Application.Authorization.Interfaces;
public interface IJwtGenerator
{
    Task<Tokens> IssueTokens(AppUser user, Character character);
    Task<Tokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a jwt access token
    /// </summary>
    /// <param name="token"></param>
    /// <returns>Validity bool</returns>
    Task<bool> ValidateAccessToken(string token);
}
