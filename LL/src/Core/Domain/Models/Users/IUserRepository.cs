namespace Domain.Models.Users;
public interface IUserRepository
{

    /// <summary>
    /// Check if the User exists through an email
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public bool DoesUserExist(string email);

    /// <summary>
    /// Check if the Guest exists through userId
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    bool DoesGuestExist(string userId);
}