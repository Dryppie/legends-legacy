using Application.Interfaces.Services.LL.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Common;

public sealed class AccountRestrictionAuthorizationResultHandler(
    IAccountRestrictionIndex restrictions)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Forbidden ||
            !ActiveAccountAuthorizationHandler.TryGetAccountId(
                context.User,
                out var accountId))
        {
            await _fallback.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var access = restrictions.Get(accountId);
        var code = !access.CanAuthenticate
            ? "account_banned"
            : !access.CanParticipate
                ? "account_multiplayer_restricted"
                : null;
        if (code is null)
        {
            await _fallback.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Account access restricted",
            Detail = code == "account_multiplayer_restricted"
                ? "Access to multiplayer and player-economy services has been restricted for this account."
                : "Access to the game has been restricted for this account.",
            Instance = context.Request.Path
        };
        details.Extensions["code"] = code;
        details.Extensions["requestId"] =
            RequestLoggingMiddleware.GetRequestId(context);
        if (access.EffectiveRestriction?.ExpiresAt is { } expiresAt)
        {
            details.Extensions["expiresAt"] = expiresAt;
        }

        await context.Response.WriteAsJsonAsync(
            details,
            cancellationToken: context.RequestAborted);
    }
}
