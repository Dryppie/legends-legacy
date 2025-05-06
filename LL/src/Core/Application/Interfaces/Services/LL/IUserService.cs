using Domain.Models.Users;

namespace Application.Interfaces.Services.LL;
public interface IUserService
{
    Task<AppUser?> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken);
    Task<AppUser?> RegisterGuestAsync(CancellationToken cancellationToken);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);
    Task<UserInfo?> GetUserInfo(Guid UserId, CancellationToken cancellationToken);


    /// <summary>
    /// Convert a guest into a user account
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<AppUser?> ConvertGuestToUser(Guid userId, string username, string email, string password, CancellationToken cancellationToken);

    ///// <summary>
    ///// Login with the given Email and Password
    ///// </summary>
    ///// <param name="email"></param>
    ///// <param name="password"></param>
    ///// <returns></returns>
    //public Task<AuthInfo> Login(string email, string password);

    ///// <summary>
    ///// Register with the given Username, Email, and Password
    ///// </summary>
    ///// <param name="username"></param>
    ///// <param name="email"></param>
    ///// <param name="password"></param>
    ///// <returns></returns>
    //public Task<AuthInfo> Register(string username, string email, string password);

    ///// <summary>
    ///// Register as a guest user
    ///// </summary>
    ///// <returns></returns>
    //public Task<AuthInfo> RegisterGuest();
}