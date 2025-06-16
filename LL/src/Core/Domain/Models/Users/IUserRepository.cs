namespace Domain.Models.Users;
public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken);
    Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken);
}