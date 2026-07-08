using Application.Interfaces.Services.LL;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace Services.LL.Users;
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<AppUser> _hasher;

    public UserService(IUserRepository userRepository, IPasswordHasher<AppUser> hasher)
    {
        _userRepository = userRepository;
        _hasher = hasher;
    }

    public async Task<AppUser?> RegisterAsync(string accountLabel, string email, string password, CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(email, null, cancellationToken))
            return null;

        var user = AppUser.Register(accountLabel, email,
                     _hasher.HashPassword(null!, password));

        var added = await _userRepository.AddAsync(user, cancellationToken);
        if (!added) return null;

        return user;
    }

    public async Task<AppUser?> RegisterGuestAsync(string accountLabel, CancellationToken cancellationToken)
    {
        var guest = AppUser.Guest();
        guest.Username = accountLabel;
        guest.NormalizeIdentityFields();

        if (await _userRepository.AddAsync(guest, cancellationToken))
        {
            return guest;
        }

        return null;
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (user == null) return null;

        var vr = _hasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (vr != PasswordVerificationResult.Success)
            return null;

        return user;
    }

    public async Task<AppUser?> ConvertGuestToUser(Guid userId, string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user == null) return null;
        if (!user.IsGuest) return null;
        if (await _userRepository.EmailExistsAsync(email, userId, cancellationToken)) return null;


        user.ConvertGuestToAccount(email,
                     _hasher.HashPassword(null!, password));

        return user;
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken) =>
        await _userRepository.EmailExistsAsync(email, excludedUserId, cancellationToken);

    public async Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetUserInfo(userId, cancellationToken);
    }

    public bool UpdateUserInfo(AppUser user)
    {
        user.IsNameEdited = true;
        return true;
    }

    public async Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetUserById(userId, cancellationToken);
    }
}
