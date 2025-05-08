using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Domain.Models.Users;

namespace Services.LL.Authorization;
public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly IGoogleTokenValidator _validator;
    private readonly IExternalLoginRepository _externals;
    private readonly IUserRepository _users;
    private readonly IUserService _userService;

    public GoogleAuthService(IGoogleTokenValidator validator, IExternalLoginRepository externals, IUserRepository users, IUserService userService)
    {
        _validator = validator;
        _externals = externals;
        _users = users;
        _userService = userService;
    }

    public async Task<GoogleLoginResult?> LoginOrCreateAsync(string idToken, CancellationToken cancellationToken)
    {
        var payload = await _validator.ValidateAsync(idToken, cancellationToken);
        var googleId = payload.Subject;

        var ext = await _externals.FindAsync(AuthProvider.Google, googleId, cancellationToken);
        if (ext is not null) return new GoogleLoginResult(ext.User, false);

        var user = await _users.FindByEmailAsync(payload.Email, cancellationToken);
        var isNew = false;

        if (user is null)
        {
            // new user
            isNew = true;
            user = await _userService.RegisterAsync(
                       username: payload.Email!.Split('@')[0],
                       email: payload.Email!,
                       password: Guid.NewGuid().ToString(), // won't be used yet
                       cancellationToken);
            if (user == null) return null;
        }

        await _externals.AddAsync(new ExternalLogin
        {
            UserId = user.Id,
            Provider = AuthProvider.Google,
            ProviderUserId = googleId
        }, cancellationToken);

        return new GoogleLoginResult(user, isNew);
    }

    public async Task<bool> BindAsync(Guid userId, string idToken, CancellationToken cancellationToken)
    {
        var payload = await _validator.ValidateAsync(idToken, cancellationToken);

        // already linked elsewhere? return false (error)
        if (await _externals.FindAsync(AuthProvider.Google, payload.Subject, cancellationToken) is not null)
            return false;

        await _externals.AddAsync(new ExternalLogin
        {
            UserId = userId,
            Provider = AuthProvider.Google,
            ProviderUserId = payload.Subject
        }, cancellationToken);
        return true;
    }
}