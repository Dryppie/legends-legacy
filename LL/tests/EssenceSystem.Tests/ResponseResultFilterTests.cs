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
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
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

        var badRequest = Assert.IsType<BadRequestObjectResult>(executedResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.Equal("Not enough crafting materials.", badRequest.Value);
    }
}
