using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.LL.Common;

public sealed class ConcurrencyExceptionHandler(
    ILogger<ConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    private const string DungeonRunCharacterIndex = "IX_DungeonRuns_CharacterId";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var duplicateDungeonStart =
            exception is DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: DungeonRunCharacterIndex
                }
            };
        if (exception is not DbUpdateConcurrencyException && !duplicateDungeonStart)
        {
            return false;
        }

        var route = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
            ?? "(unmatched)";
        logger.LogWarning(
            exception,
            "A concurrent update prevented duplicate command processing for {HttpRoute}.",
            route);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Game state changed",
                Detail = duplicateDungeonStart
                    ? "A dungeon run is already active for this character. Refresh to continue it."
                    : "This action was already updated by another request. Refresh and try again.",
                Extensions =
                {
                    ["requestId"] = RequestLoggingMiddleware.GetRequestId(httpContext)
                }
            },
            cancellationToken);

        return true;
    }
}
