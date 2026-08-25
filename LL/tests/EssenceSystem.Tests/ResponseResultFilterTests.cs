using API.LL.Filters;
using Common.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace EssenceSystem.Tests;

public sealed class ResponseResultFilterTests
{
    [Fact]
    public async Task Expected_business_failure_remains_bad_request()
    {
        var filter = new ResponseResultFilter();
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "request-business-400"
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var context = new ResultExecutingContext(
            actionContext,
            [],
            new ObjectResult(Response<int>.Fail("Not enough crafting materials.")),
            new object());

        IActionResult? executedResult = null;
        await filter.OnResultExecutionAsync(
            context,
            () =>
            {
                executedResult = context.Result;
                return Task.FromResult(new ResultExecutedContext(
                    actionContext,
                    context.Filters,
                    context.Result,
                    context.Controller));
            });

        var badRequest = Assert.IsType<ObjectResult>(executedResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Not enough crafting materials.", details.Detail);
        Assert.Equal(
            Response<int>.DefaultErrorCode,
            details.Extensions["code"]);
        Assert.Equal("business", details.Extensions["category"]);
        Assert.Equal(
            "Not enough crafting materials.",
            details.Extensions["message"]);
        Assert.Equal("request-business-400", details.Extensions["requestId"]);
    }

    [Fact]
    public async Task Expected_business_failure_can_supply_a_stable_code()
    {
        var filter = new ResponseResultFilter();
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "request-coded-400"
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var context = new ResultExecutingContext(
            actionContext,
            [],
            new ObjectResult(Response<int>.Fail(
                "Not enough crafting materials.",
                "insufficient_materials")),
            new object());

        IActionResult? executedResult = null;
        await filter.OnResultExecutionAsync(
            context,
            () =>
            {
                executedResult = context.Result;
                return Task.FromResult(new ResultExecutedContext(
                    actionContext,
                    context.Filters,
                    context.Result,
                    context.Controller));
            });

        var result = Assert.IsType<ObjectResult>(executedResult);
        var details = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("insufficient_materials", details.Extensions["code"]);
    }

    [Fact]
    public async Task Expected_conflict_returns_conflict_problem_details()
    {
        var filter = new ResponseResultFilter();
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "request-conflict-409"
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var context = new ResultExecutingContext(
            actionContext,
            [],
            new ObjectResult(Response<int>.Conflict(
                "An Essence loadout with that name already exists.",
                "essence_loadout_name_conflict")),
            new object());

        IActionResult? executedResult = null;
        await filter.OnResultExecutionAsync(
            context,
            () =>
            {
                executedResult = context.Result;
                return Task.FromResult(new ResultExecutedContext(
                    actionContext,
                    context.Filters,
                    context.Result,
                    context.Controller));
            });

        var result = Assert.IsType<ObjectResult>(executedResult);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        var details = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("Conflict", details.Title);
        Assert.Equal("conflict", details.Extensions["category"]);
        Assert.Equal(
            "essence_loadout_name_conflict",
            details.Extensions["code"]);
    }
}
