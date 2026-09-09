using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BalanceHarness;

public static class TowerDashboardServer
{
    public static WebApplication Create(TowerDashboardService service, int port)
    {
        if (port is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ContentRootPath = AppContext.BaseDirectory });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port);
            options.Limits.MaxRequestBodySize = 16384;
        });
        var app = builder.Build();
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'";
            var host = context.Request.Host;
            var origin = context.Request.Headers.Origin.ToString();
            if (host.Host is not ("127.0.0.1" or "localhost") || host.Port != context.Connection.LocalPort
                || (origin.Length > 0 && origin != $"http://{host}")
                || (context.Request.Method != "GET" && context.Request.Headers["X-Tower-Session"] != token))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Only this local dashboard session can make this request." });
                return;
            }
            try { await next(context); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (Exception error)
            {
                context.Response.StatusCode = error is InvalidOperationException ? 409 : 400;
                await context.Response.WriteAsJsonAsync(new { error = error.Message });
            }
        });
        foreach (var (route, resource, type) in new[]
        {
            ("/", "index.html", "text/html"), ("/dashboard.css", "dashboard.css", "text/css"),
            ("/dashboard.js", "dashboard.js", "text/javascript")
        })
            app.MapGet(route, () => Results.Stream(typeof(TowerDashboardServer).Assembly
                .GetManifestResourceStream($"BalanceHarness.Dashboard.{resource}")!, type));
        app.MapGet("/api/session", () => Results.Json(new { token, catalogs = service.Catalogs(), floors = service.Floors() }, HarnessJson.Options));
        app.MapGet("/api/runs", () => Results.Json(service.Runs(), HarnessJson.Options));
        app.MapGet("/api/search-plan", () => Results.Json(service.SearchPlan(), HarnessJson.Options));
        app.MapPost("/api/search", () => Results.Json(service.Search(), HarnessJson.Options, statusCode: 202));
        app.MapGet("/api/runs/{id}/search/{format}", async (string id, string format, CancellationToken cancellation) =>
        {
            if (format is not ("json" or "md")) throw new InvalidDataException("Choose JSON or Markdown.");
            var report = await Task.Run(() => service.SearchReport(id, cancellation), cancellation);
            return Results.Text(format == "md" ? TowerEssenceSearch.Markdown(report) : System.Text.Json.JsonSerializer.Serialize(report, HarnessJson.Options),
                format == "md" ? "text/markdown" : "application/json");
        });
        app.MapGet("/api/job", () => service.Job is { } job
            ? (IResult)Results.Json(job, HarnessJson.Options) : Results.Text("null", "application/json"));
        app.MapGet("/api/runs/{id}", async (string id, CancellationToken cancellation) =>
            Results.Json(await Task.Run(() => service.Details(id, cancellation), cancellation), HarnessJson.Options));
        app.MapPost("/api/runs", async (HttpRequest request) => Results.Json(service.Start(
            await request.ReadFromJsonAsync<DashboardRunRequest>(HarnessJson.Options) ?? throw new InvalidDataException("Missing run selection.")), HarnessJson.Options, statusCode: 202));
        app.MapPost("/api/replays", async (HttpRequest request) => Results.Json(service.Replay(
            await request.ReadFromJsonAsync<DashboardReplayRequest>(HarnessJson.Options) ?? throw new InvalidDataException("Missing replay selection.")), HarnessJson.Options, statusCode: 202));
        app.MapPost("/api/jobs/{id}/cancel", (string id) => Results.Json(service.Cancel(id), HarnessJson.Options));
        app.MapGet("/api/jobs/{id}/replay", (string id) => Results.File(service.ReplayFile(id), "application/json", "verified-replay.json"));
        app.MapGet("/api/runs/{id}/download/{format}", (string id, string format, CancellationToken cancellation) =>
        {
            if (format is not ("json" or "md")) throw new InvalidDataException("Choose a JSON or Markdown report.");
            var run = service.ResolveRun(id);
            TowerBenchmark.ReadSaved(run, cancellation);
            return Results.File(Path.Combine(run, "benchmark." + format), format == "json" ? "application/json" : "text/markdown", "tower-benchmark." + format);
        });
        return app;
    }

    public static async Task RunAsync(string apiRoot, string catalogsRoot, string runsRoot, int port, CancellationToken token)
    {
        await using var service = new TowerDashboardService(apiRoot, catalogsRoot, runsRoot);
        await using var app = Create(service, port);
        await app.StartAsync(token);
        Console.WriteLine($"Tower dashboard: {app.Urls.Single()}");
        Console.WriteLine($"Results folder: {Path.GetFullPath(runsRoot)}. Press Ctrl+C to stop and cancel any active operation.");
        await app.WaitForShutdownAsync(token);
    }
}
