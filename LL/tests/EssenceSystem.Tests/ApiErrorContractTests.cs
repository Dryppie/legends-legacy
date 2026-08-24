using API.LL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssenceSystem.Tests;

public sealed class ApiErrorContractTests
{
    [Fact]
    public void Unexpected_failure_gets_a_safe_correlated_contract()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "request-unexpected-500"
        };
        context.Request.Path = "/api/v1/Crafting/Craft";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "NpgsqlException",
            Detail = "database.internal:5432 timed out"
        };

        ApiErrorContract.Enrich(details, context);

        Assert.Equal("Unexpected error", details.Title);
        Assert.Equal("An unexpected error occurred.", details.Detail);
        Assert.Equal("/api/v1/Crafting/Craft", details.Instance);
        Assert.Equal("unexpected_error", details.Extensions["code"]);
        Assert.Equal("system", details.Extensions["category"]);
        Assert.Equal(
            "An unexpected error occurred.",
            details.Extensions["message"]);
        Assert.Equal(
            "request-unexpected-500",
            details.Extensions["requestId"]);
    }

    [Fact]
    public void Existing_specific_error_identity_is_preserved()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "request-specific-409"
        };
        var details = ApiErrorContract.Create(
            context,
            StatusCodes.Status409Conflict,
            "Game state changed",
            "Refresh and try again.",
            "concurrent_update",
            ApiErrorContract.ConflictCategory);

        ApiErrorContract.Enrich(details, context);

        Assert.Equal("concurrent_update", details.Extensions["code"]);
        Assert.Equal("conflict", details.Extensions["category"]);
        Assert.Equal("Refresh and try again.", details.Extensions["message"]);
    }
}
