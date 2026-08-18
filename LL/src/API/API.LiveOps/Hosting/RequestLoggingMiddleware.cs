using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace API.LiveOps.Hosting;

public sealed class RequestLoggingMiddleware
{
    public const string RequestIdHeaderName = "X-Request-ID";

    private const string RequestIdItemKey = "LegendsLegacy.RequestId";
    private static readonly EventId RequestCompleted = new(1000, nameof(RequestCompleted));
    private static readonly HashSet<string> StaticAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".gif", ".ico", ".jpeg", ".jpg", ".js", ".json", ".map",
        ".png", ".svg", ".webp", ".woff", ".woff2"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly double _slowRequestThresholdMs;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = Math.Max(
            1,
            configuration.GetValue<double?>("RequestLogging:SlowRequestThresholdMs") ?? 2_000);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetRequestId(context);
        context.Items[RequestIdItemKey] = requestId;
        context.Response.Headers[RequestIdHeaderName] = requestId;

        var activity = Activity.Current;
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = activity?.TraceId.ToString() ?? requestId,
            ["SpanId"] = activity?.SpanId.ToString(),
            ["RequestId"] = requestId
        });

        var startedAt = Stopwatch.GetTimestamp();
        await _next(context);

        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        if (ShouldSkip(context))
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        var level = statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : statusCode == StatusCodes.Status429TooManyRequests || elapsedMs >= _slowRequestThresholdMs
                ? LogLevel.Warning
                : statusCode >= StatusCodes.Status400BadRequest
                    ? LogLevel.Information
                    : LogLevel.Debug;

        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
            ?? "(unmatched)";

        _logger.Log(
            level,
            RequestCompleted,
            "HTTP {HttpMethod} {HttpRoute} responded {HttpStatusCode} in {DurationMs} ms.",
            context.Request.Method,
            route,
            statusCode,
            Math.Round(elapsedMs, 3));
    }

    public static string GetRequestId(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestIdItemKey, out var existing)
            && existing is string requestId
            && !string.IsNullOrWhiteSpace(requestId))
        {
            return requestId;
        }

        var traceId = Activity.Current?.TraceId;
        return traceId.HasValue && traceId.Value != default
            ? traceId.Value.ToString()
            : context.TraceIdentifier;
    }

    private static bool ShouldSkip(HttpContext context)
    {
        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return false;
        }

        if (context.Request.Path.StartsWithSegments("/healthz/live")
            || context.Request.Path.StartsWithSegments("/healthz/ready"))
        {
            return true;
        }

        if (context.Request.Path.StartsWithSegments("/hub"))
        {
            return true;
        }

        var extension = Path.GetExtension(context.Request.Path.Value);
        return !string.IsNullOrEmpty(extension) && StaticAssetExtensions.Contains(extension);
    }
}
