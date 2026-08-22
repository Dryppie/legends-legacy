using System.Text.Json.Serialization;
using API.LiveOps.Authorization;
using API.LiveOps.Chat;
using API.LiveOps.Health;
using API.LiveOps.Hosting;
using API.LiveOps.Previews;
using API.LiveOps.Support;
using Application.Interfaces.Services.LL.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Persistence.LL;
using Services.LL;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var staffAuthority = config["StaffIdentity:Authority"] ?? string.Empty;
var staffAudience = config["StaffIdentity:Audience"] ?? string.Empty;
ValidateProductionConfiguration(
    builder.Environment,
    staffAuthority,
    staffAudience,
    config);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CookieAntiforgeryFilter>();
builder.Services.AddControllers(options =>
    options.Filters.AddService<CookieAntiforgeryFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["requestId"] =
            RequestLoggingMiddleware.GetRequestId(context.HttpContext);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("audit-exports", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "LL-LiveOps-XSRF"
        : "__Host-LL-LiveOps-XSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddLiveOpsApplication();
builder.Services.AddLiveOpsServices(config);
builder.Services.AddLiveOpsForwardedHeaders(config);

builder.Services.Configure<ChatModerationOptions>(
    config.GetSection(ChatModerationOptions.SectionName));
builder.Services.AddHttpClient<IChatModerationGateway, ChatModerationGateway>();
builder.Services.AddHealthChecks()
    .AddCheck<LiveOpsDatabaseHealthCheck>(
        "game_database",
        tags: ["ready"])
    .AddCheck<ChatModerationHealthCheck>(
        "chat_moderation",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: ["ready"]);
builder.Services.AddScoped<LiveOpsOperationalStatusService>();
builder.Services.AddScoped<ILiveOpsRecentActivityReader, LiveOpsRecentActivityReader>();
builder.Services.AddScoped<LiveOpsActionPreviewService>();
builder.Services.AddScoped<LiveOpsPlayerSupportSnapshotService>();
builder.Services.AddScoped<TransferConversationCorrelationService>();
builder.Services.AddHostedService<AccountRiskEvaluationWorker>();

var allowedOrigins = config.GetSection("LiveOps:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy(
        "LiveOpsOrigins",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(RequestLoggingMiddleware.RequestIdHeaderName)));
}

builder.Services.AddLiveOpsAuthentication(
    config,
    builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddLiveOpsPolicies();
});

var app = builder.Build();

if (config.GetValue<bool>($"{LiveOpsReverseProxy.SectionName}:Enabled"))
{
    app.UseForwardedHeaders();
}
app.UseLiveOpsPublicOrigin(config);
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; " +
        "form-action 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; connect-src 'self'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

if (allowedOrigins.Length > 0)
{
    app.UseCors("LiveOpsOrigins");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

static void ValidateProductionConfiguration(
    IWebHostEnvironment environment,
    string staffAuthority,
    string staffAudience,
    IConfiguration configuration)
{
    if (environment.IsDevelopment())
    {
        return;
    }

    if (configuration.GetValue<bool>("LiveOps:DevelopmentOperator:Enabled"))
    {
        throw new InvalidOperationException(
            "The LiveOps development operator must be disabled outside Development.");
    }

    if (!Uri.TryCreate(staffAuthority, UriKind.Absolute, out var authorityUri) ||
        authorityUri.Scheme != Uri.UriSchemeHttps ||
        authorityUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "StaffIdentity:Authority must be a real HTTPS OIDC authority outside Development.");
    }
    if (string.IsNullOrWhiteSpace(staffAudience))
    {
        throw new InvalidOperationException(
            "StaffIdentity:Audience is required outside Development.");
    }
    if (string.IsNullOrWhiteSpace(configuration["StaffIdentity:ClientId"]) ||
        string.IsNullOrWhiteSpace(configuration["StaffIdentity:ClientSecret"]))
    {
        throw new InvalidOperationException(
            "StaffIdentity client ID and client secret are required outside Development.");
    }
    if (string.IsNullOrWhiteSpace(configuration["StaffIdentity:OwnerSubject"]) &&
        string.IsNullOrWhiteSpace(configuration["StaffIdentity:BootstrapOwnerEmail"]))
    {
        throw new InvalidOperationException(
            "A staff owner subject or bootstrap owner email is required outside Development.");
    }

    var allowedHosts = (configuration["AllowedHosts"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (allowedHosts.Length == 0 ||
        allowedHosts.Any(host =>
            host == "*" ||
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException(
            "AllowedHosts must contain the private LiveOps hostname outside Development.");
    }
    if (!LiveOpsPublicOrigin.TryParse(
            configuration[LiveOpsPublicOrigin.ConfigurationKey],
            out var publicBaseUri) ||
        publicBaseUri.Scheme != Uri.UriSchemeHttps ||
        !allowedHosts.Contains(publicBaseUri.Host, StringComparer.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "LiveOps:PublicBaseUrl must be an HTTPS root URL whose host is present in AllowedHosts outside Development.");
    }
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("LegendsLegacyDB")))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:LegendsLegacyDB is required outside Development.");
    }

    var reverseProxy = configuration.GetSection(LiveOpsReverseProxy.SectionName);
    var knownProxies = reverseProxy.GetSection("KnownProxies").Get<string[]>() ?? [];
    var knownNetworks = reverseProxy.GetSection("KnownNetworks").Get<string[]>() ?? [];
    if (!reverseProxy.GetValue<bool>("Enabled") ||
        knownProxies.Length + knownNetworks.Length == 0)
    {
        throw new InvalidOperationException(
            "A trusted reverse proxy must be enabled and configured outside Development.");
    }
    if (knownProxies.Any(proxy => !System.Net.IPAddress.TryParse(proxy, out _)))
    {
        throw new InvalidOperationException(
            "Every ReverseProxy:KnownProxies entry must be an IP address.");
    }
    if (knownNetworks.Any(network => !System.Net.IPNetwork.TryParse(network, out _)))
    {
        throw new InvalidOperationException(
            "Every ReverseProxy:KnownNetworks entry must use CIDR notation.");
    }

    var chatBaseUrl = configuration["Chat:Moderation:BaseUrl"];
    var chatSecret = configuration["Chat:Moderation:Secret"];
    if (!Uri.TryCreate(chatBaseUrl, UriKind.Absolute, out _) ||
        string.IsNullOrWhiteSpace(chatSecret))
    {
        throw new InvalidOperationException(
            "Chat moderation base URL and secret are required outside Development.");
    }
}
