using System.Diagnostics;
using System.Security.Claims;
using API.LL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EssenceSystem.Tests;

public sealed class RequestLoggingMiddlewareTests
{
    [Theory]
    [InlineData(StatusCodes.Status200OK, LogLevel.Debug)]
    [InlineData(StatusCodes.Status401Unauthorized, LogLevel.Information)]
    [InlineData(StatusCodes.Status409Conflict, LogLevel.Information)]
    [InlineData(StatusCodes.Status429TooManyRequests, LogLevel.Warning)]
    [InlineData(StatusCodes.Status500InternalServerError, LogLevel.Error)]
    public async Task Completion_log_level_matches_response_status(int statusCode, LogLevel expectedLevel)
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            },
            logger,
            CreateConfiguration());
        var context = new DefaultHttpContext();
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/test"),
            0,
            EndpointMetadataCollection.Empty,
            "test endpoint"));

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedLevel, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task Slow_successful_request_is_warning()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            async _ => await Task.Delay(20),
            logger,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RequestLogging:SlowRequestThresholdMs"] = "1"
                })
                .Build());
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task Completion_log_uses_trace_id_route_template_and_safe_fields()
    {
        using var activity = new Activity("request-test")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            },
            logger,
            CreateConfiguration());
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/items/sensitive-item-id";
        context.Request.QueryString = new QueryString("?access_token=super-secret-query");
        context.Request.Headers.Authorization = "Bearer super-secret-token";
        context.Request.Headers.Cookie = "session=super-secret-cookie";
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/items/{id}"),
            0,
            EndpointMetadataCollection.Empty,
            "test endpoint"));

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(1000, entry.EventId.Id);
        Assert.Equal("/api/v1/items/{id}", entry.Properties["HttpRoute"]);
        Assert.Equal(StatusCodes.Status500InternalServerError, entry.Properties["HttpStatusCode"]);
        Assert.Equal(activity.TraceId.ToString(), context.Response.Headers[RequestLoggingMiddleware.RequestIdHeaderName]);

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(activity.TraceId.ToString(), scope["TraceId"]);
        Assert.Equal(activity.SpanId.ToString(), scope["SpanId"]);
        Assert.Equal(activity.TraceId.ToString(), scope["RequestId"]);

        var captured = logger.CapturedText();
        Assert.DoesNotContain("sensitive-item-id", captured, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-query", captured, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token", captured, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-cookie", captured, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_health_check_has_request_id_without_completion_log()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger,
            CreateConfiguration());
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "health-request";
        context.Request.Path = "/healthz/ready";

        await middleware.InvokeAsync(context);

        Assert.Equal("health-request", context.Response.Headers[RequestLoggingMiddleware.RequestIdHeaderName]);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Successful_signal_r_transport_has_request_id_without_completion_log()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger,
            CreateConfiguration());
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "signalr-request";
        context.Request.Path = "/hub/game";
        context.Request.QueryString = new QueryString("?access_token=super-secret-token");

        await middleware.InvokeAsync(context);

        Assert.Equal("signalr-request", context.Response.Headers[RequestLoggingMiddleware.RequestIdHeaderName]);
        Assert.Empty(logger.Entries);
        Assert.DoesNotContain("super-secret-token", logger.CapturedText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_health_check_is_logged()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            },
            logger,
            CreateConfiguration());
        var context = new DefaultHttpContext();
        context.Request.Path = "/healthz/ready";

        await middleware.InvokeAsync(context);

        Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task Authenticated_identity_scope_contains_only_stable_identifiers()
    {
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var logger = new CapturingLogger<AuthenticatedIdentityLoggingMiddleware>();
        var nextCalled = false;
        var middleware = new AuthenticatedIdentityLoggingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            logger);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.UserData, accountId.ToString()),
                new Claim("CharacterId", characterId.ToString()),
                new Claim(ClaimTypes.Email, "private@example.test")
            ], "test"))
        };

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(accountId, scope["AccountId"]);
        Assert.Equal(characterId, scope["CharacterId"]);
        Assert.DoesNotContain("private@example.test", logger.CapturedText(), StringComparison.Ordinal);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RequestLogging:SlowRequestThresholdMs"] = "60000"
            })
            .Build();

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(ToDictionary(state));
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), ToDictionary(state)));
        }

        public string CapturedText() => string.Join(
            " ",
            Entries.SelectMany(entry => entry.Properties.Select(pair => $"{pair.Key}={pair.Value}"))
                .Concat(Entries.Select(entry => entry.Message))
                .Concat(Scopes.SelectMany(scope => scope.Select(pair => $"{pair.Key}={pair.Value}"))));

        private static IReadOnlyDictionary<string, object?> ToDictionary<TState>(TState state)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                return properties.ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            return new Dictionary<string, object?> { ["Scope"] = state };
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
