using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.LL.Common;

public sealed class ConcurrencyExceptionHandler(
    ILogger<ConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        logger.LogWarning(
            exception,
            "A concurrent update prevented duplicate command processing for {Path}.",
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Game state changed",
                Detail = "This action was already updated by another request. Refresh and try again."
            },
            cancellationToken);

        return true;
    }
}
