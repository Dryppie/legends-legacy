using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.LL.Common;

public sealed class DatabaseConflictExceptionHandler(
    ILogger<DatabaseConflictExceptionHandler> logger) : IExceptionHandler
{
    internal const string EssenceLoadoutNameIndex = "IX_EssenceLoadouts_CharacterId_Name";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var duplicateLoadoutName =
            exception is DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: EssenceLoadoutNameIndex
                }
            };
        if (!duplicateLoadoutName)
        {
            return false;
        }

        var route = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
            ?? "(unmatched)";
        logger.LogWarning(
            exception,
            "A database uniqueness conflict rejected the request for {HttpRoute}.",
            route);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            ApiErrorContract.Create(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                "An Essence loadout with that name already exists.",
                "essence_loadout_name_conflict",
                ApiErrorContract.ConflictCategory),
            cancellationToken);

        return true;
    }
}
