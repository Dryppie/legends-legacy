using Microsoft.AspNetCore.Authorization;

namespace API.LL.Common;

public static class AuthorizationPolicies
{
    public const string RegisteredUser = nameof(RegisteredUser);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            RegisteredUser,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("guest", bool.FalseString));
    }
}
