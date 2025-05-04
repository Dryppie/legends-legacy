namespace Domain.Models.Users;
public interface IUserRepository
{
    /// <summary>
    /// Check if the User exists through an email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public bool DoesEmailExist(string email);

    /// <summary>
    /// Check if the User exists through the username
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public bool DoesUsernameExist(string email);

    /// <summary>
    /// Check if the Guest exists through userId
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    bool DoesGuestExist(string userId);

    public Task<UserInfo> GetUserInfo(Guid userId);
}