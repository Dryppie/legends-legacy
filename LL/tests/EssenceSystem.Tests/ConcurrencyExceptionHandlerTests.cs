using API.LL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class ConcurrencyExceptionHandlerTests
{
    [Fact]
    public async Task Character_action_conflict_returns_recoverable_problem_details()
    {
        var handler = new ConcurrencyExceptionHandler(
            NullLogger<ConcurrencyExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/CharacterActions/Resolve";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new DbUpdateConcurrencyException("conflict"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            "This action was already updated by another request. Refresh and try again.",
            body.RootElement.GetProperty("detail").GetString());
    }
}
