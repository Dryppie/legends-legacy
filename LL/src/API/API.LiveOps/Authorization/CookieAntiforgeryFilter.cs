using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.LiveOps.Authorization;

public sealed class CookieAntiforgeryFilter(IAntiforgery antiforgery)
    : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Options,
            HttpMethods.Trace
        };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method) ||
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new
            {
                code = "antiforgery-validation-failed",
                message = "The operator session changed. Refresh the page and try again."
            });
        }
    }
}
