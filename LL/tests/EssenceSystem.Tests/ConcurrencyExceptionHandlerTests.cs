using API.LL.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class ConcurrencyExceptionHandlerTests
{
    [Fact]
    public async Task Conflict_logs_original_route_after_exception_middleware_clears_endpoint()
    {
        var logger = new CapturingLogger();
        var handler = new ConcurrencyExceptionHandler(logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new DbUpdateConcurrencyException("conflict");
        const string route = "api/v1/equipment/loadouts/{id:guid}/apply";
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = exception,
            Endpoint = new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse(route), 0, EndpointMetadataCollection.Empty, "ApplyLoadout")
        });

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        Assert.Equal(route, logger.Properties["HttpRoute"]);
        Assert.DoesNotContain("duplicate command", logger.Message);
        Assert.Same(exception, logger.Exception);
    }

    [Fact]
    public async Task Character_action_conflict_returns_recoverable_problem_details()
    {
        var handler = new ConcurrencyExceptionHandler(
            NullLogger<ConcurrencyExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "request-409";
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
        Assert.Equal(
            "request-409",
            body.RootElement.GetProperty("requestId").GetString());
        Assert.Equal(
            "concurrent_update",
            body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            "conflict",
            body.RootElement.GetProperty("category").GetString());
        Assert.Equal(
            body.RootElement.GetProperty("detail").GetString(),
            body.RootElement.GetProperty("message").GetString());
    }
    private sealed class CapturingLogger : ILogger<ConcurrencyExceptionHandler>
    {
        public Dictionary<string, object?> Properties { get; private set; } = [];
        public string Message { get; private set; } = string.Empty;
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Properties = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary();
            Message = formatter(state, exception);
            Exception = exception;
        }
    }
}
