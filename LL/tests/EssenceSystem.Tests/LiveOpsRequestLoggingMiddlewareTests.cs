using System.Diagnostics;
using API.LiveOps.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EssenceSystem.Tests;

public sealed class LiveOpsRequestLoggingMiddlewareTests
{
    [Fact]
    public async Task Handled_exception_logs_original_route_and_exception()
    {
        var logger = new CapturingLogger();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddProblemDetails();
        services.AddSingleton(new DiagnosticListener("LiveOpsRequestLoggingTests"));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ILogger<RequestLoggingMiddleware>>(logger);
        await using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();
        var failure = new InvalidOperationException("The request pipeline could not be resolved.");
        app.Run(context =>
        {
            context.SetEndpoint(CreateEndpoint("api/liveops/account-risk/{accountId:guid}"));
            throw failure;
        });
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/liveops/account-risk/private-account-id";
        context.Request.QueryString = new QueryString("?search=private-search");
        context.Request.Headers.Authorization = "Bearer private-token";
        context.Request.Headers.Cookie = "session=private-cookie";
        context.Response.Body = new MemoryStream();

        await app.Build()(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Null(context.GetEndpoint());
        Assert.Same(failure, context.Features.Get<IExceptionHandlerFeature>()?.Error);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(1000, entry.EventId.Id);
        Assert.Equal("api/liveops/account-risk/{accountId:guid}", entry.Properties["HttpRoute"]);
        Assert.Equal(HttpMethods.Get, entry.Properties["HttpMethod"]);
        Assert.Equal(StatusCodes.Status500InternalServerError, entry.Properties["HttpStatusCode"]);
        Assert.Same(failure, entry.Exception);
        Assert.DoesNotContain("private-", entry.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("api/liveops/account-risk", StatusCodes.Status200OK, LogLevel.Debug)]
    [InlineData(null, StatusCodes.Status404NotFound, LogLevel.Information)]
    [InlineData("api/liveops/account-risk", StatusCodes.Status500InternalServerError, LogLevel.Error)]
    public async Task Responses_without_exceptions_keep_route_and_status_logging(
        string? route,
        int statusCode,
        LogLevel level)
    {
        var logger = new CapturingLogger();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.SetEndpoint(route is null ? null : CreateEndpoint(route));
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            },
            logger,
            new ConfigurationBuilder().Build());

        await middleware.InvokeAsync(new DefaultHttpContext());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(level, entry.Level);
        Assert.Equal(route ?? "(unmatched)", entry.Properties["HttpRoute"]);
        Assert.Null(entry.Exception);
    }

    private static RouteEndpoint CreateEndpoint(string route) => new(
        _ => Task.CompletedTask,
        RoutePatternFactory.Parse(route),
        0,
        EndpointMetadataCollection.Empty,
        "test endpoint");

    private sealed class CapturingLogger : ILogger<RequestLoggingMiddleware>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception,
                properties.ToDictionary(pair => pair.Key, pair => pair.Value)));
        }
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message,
        Exception? Exception, IReadOnlyDictionary<string, object?> Properties);
}
