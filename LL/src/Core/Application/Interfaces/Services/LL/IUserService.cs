using Domain.Models.Users;

namespace Application.Interfaces.Services.LL;
public interface IUserService
{
    Task<AppUser?> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken);
    Task<AppUser?> RegisterGuestAsync(CancellationToken cancellationToken);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, Guid? excludedUserId, CancellationToken cancellationToken);
    Task<UserInfo?> GetUserInfo(Guid UserId, CancellationToken cancellationToken);


    /// <summary>
    /// Convert a guest into a user account
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<AppUser?> ConvertGuestToUser(Guid userId, string username, string email, string password, CancellationToken cancellationToken);
    bool UpdateUserInfo( AppUser user);
    Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken);
}
