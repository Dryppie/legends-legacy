using Domain.Models.Users;

namespace Application.Authorization.Interfaces;
public sealed record GoogleLoginResult(AppUser User, bool IsNewAccount);

public interface IGoogleAuthService
{
    Task<GoogleLoginResult?> LoginOrCreateAsync(string idToken, CancellationToken cancellationToken);
    Task<bool> BindAsync(Guid userId, string idToken, CancellationToken cancellationToken);
}