using Domain.Models.CombatStyles;
using Microsoft.AspNetCore.Diagnostics;

namespace API.LL.Common;

public sealed class CombatStyleConfigurationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not CombatStyleConfigurationException) return false;
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(ApiErrorContract.Create(context, StatusCodes.Status409Conflict,
            "Choose a valid Combat Style configuration", exception.Message, "combat_style_configuration_invalid",
            ApiErrorContract.ConflictCategory), ct);
        return true;
    }
}
