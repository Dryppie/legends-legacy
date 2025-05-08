namespace Domain.Models.Users;
public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(AppUser user, CancellationToken cancellationToken);
    Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Check if the User exists through an email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    //public bool DoesEmailExist(string email);

    ///// <summary>
    ///// Check if the User exists through the username
    ///// </summary>
    ///// <param name="email"></param>
    ///// <returns></returns>
    //public bool DoesUsernameExist(string email);

    ///// <summary>
    ///// Check if the Guest exists through userId
    ///// </summary>
    ///// <param name="userId"></param>
    ///// <returns></returns>
    //bool DoesGuestExist(string userId);

}