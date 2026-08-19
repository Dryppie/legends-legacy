using System.Security.Claims;
using Application.Interfaces.Services.LL.Administration;
using Microsoft.AspNetCore.Authorization;

namespace API.LL.Common;

public sealed class ActiveAccountRequirement : IAuthorizationRequirement;

public sealed class MultiplayerAllowedRequirement : IAuthorizationRequirement;

public sealed class ActiveAccountAuthorizationHandler(
    IAccountRestrictionIndex restrictions)
    : AuthorizationHandler<ActiveAccountRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveAccountRequirement requirement)
    {
        if (TryGetAccountId(context.User, out var accountId) &&
            restrictions.Get(accountId).CanAuthenticate)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    internal static bool TryGetAccountId(ClaimsPrincipal user, out Guid accountId) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.UserData), out accountId);
}

public sealed class MultiplayerAllowedAuthorizationHandler(
    IAccountRestrictionIndex restrictions)
    : AuthorizationHandler<MultiplayerAllowedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MultiplayerAllowedRequirement requirement)
    {
        if (ActiveAccountAuthorizationHandler.TryGetAccountId(context.User, out var accountId))
        {
            var access = restrictions.Get(accountId);
            if (access.CanAuthenticate && access.CanParticipate)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
