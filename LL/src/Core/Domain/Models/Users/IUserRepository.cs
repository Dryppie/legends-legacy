namespace Domain.Models.Users;
public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, Guid? excludedUserId, CancellationToken cancellationToken);
    Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken);
    Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken);
    Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken);
}
