using Microsoft.AspNetCore.Authorization;

namespace API.LL.Common;

public static class AuthorizationPolicies
{
    public const string RegisteredUser = nameof(RegisteredUser);
    public const string MultiplayerAllowed = nameof(MultiplayerAllowed);

    public static void Configure(AuthorizationOptions options)
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ActiveAccountRequirement())
            .Build();
        options.AddPolicy(
            RegisteredUser,
            policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveAccountRequirement())
                .RequireClaim("guest", bool.FalseString));
        options.AddPolicy(
            MultiplayerAllowed,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("guest", bool.FalseString)
                .AddRequirements(
                    new ActiveAccountRequirement(),
                    new MultiplayerAllowedRequirement()));
    }
}
