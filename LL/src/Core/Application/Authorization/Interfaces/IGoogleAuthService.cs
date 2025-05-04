using Domain.Models.Users;

namespace Application.Authorization.Interfaces;
public interface IGoogleAuthService
{
    Task<AppUser> LoginOrCreateAsync(string idToken, CancellationToken cancellationToken);
    Task<bool> BindAsync(Guid userId, string idToken, CancellationToken cancellationToken);
}