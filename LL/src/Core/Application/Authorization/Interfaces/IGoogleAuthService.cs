using Domain.Models.Users;

namespace Application.Authorization.Interfaces;
public sealed record GoogleLoginResult(AppUser User, bool IsNewAccount);
public sealed record GoogleBindResult(AppUser User, bool AlreadyBound);

public interface IGoogleAuthService
{
    Task<GoogleLoginResult?> LoginOrCreateAsync(string idToken, CancellationToken cancellationToken);
    Task<GoogleBindResult?> BindAsync(Guid userId, string idToken, CancellationToken cancellationToken);
}
