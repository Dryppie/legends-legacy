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
            user = await RegisterGoogleUserAsync(payload.Email!, cancellationToken);
            if (user == null) return null;
        }

        _externals.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = AuthProvider.Google,
            ProviderUserId = googleId
        });

        return new GoogleLoginResult(user, isNew);
    }

    public async Task<GoogleBindResult?> BindAsync(Guid userId, string idToken, CancellationToken cancellationToken)
    {
        var payload = await _validator.ValidateAsync(idToken, cancellationToken);

        var existingExternal = await _externals.FindAsync(AuthProvider.Google, payload.Subject, cancellationToken);
        if (existingExternal is not null)
        {
            return existingExternal.UserId == userId
                ? new GoogleBindResult(existingExternal.User, true)
                : null;
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null) return null;

        var emailOwner = await _users.FindByEmailAsync(payload.Email, cancellationToken);
        if (emailOwner is not null && emailOwner.Id != userId)
            return null;

        _externals.Add(new ExternalLogin
        {
            UserId = userId,
            Provider = AuthProvider.Google,
            ProviderUserId = payload.Subject
        });

        if (user.IsGuest)
        {
            user.ConvertGuestToExternalAccount(user.Username, payload.Email);
        }
        else
        {
            user.ConfirmExternalEmail(payload.Email);
        }

        return new GoogleBindResult(user, false);
    }

    private async Task<AppUser?> RegisterGoogleUserAsync(string email, CancellationToken cancellationToken)
    {
        var baseUsername = CreateGoogleUsername(email);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = attempt == 0 ? string.Empty : Random.Shared.Next(1000, 10000).ToString();
            var maxBaseLength = 26 - suffix.Length;
            var usernameBase = baseUsername[..Math.Min(baseUsername.Length, maxBaseLength)];
            var username = $"{usernameBase}{suffix}";

            var user = await _userService.RegisterAsync(
                username,
                email,
                Guid.NewGuid().ToString(),
                cancellationToken);

            if (user is not null)
            {
                user.EmailConfirmed = true;
                return user;
            }
        }

        return null;
    }

    private static string CreateGoogleUsername(string email)
    {
        var prefix = email.Split('@')[0].Trim();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "Player";
        }

        return prefix.Length <= 26 ? prefix : prefix[..26];
    }
}
