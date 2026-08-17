using System.Text.Json.Serialization;
using API.LiveOps.Authorization;
using API.LiveOps.Chat;
using Application;
using Application.Interfaces.Services.LL.Administration;
using Common;
using Microsoft.AspNetCore.Authorization;
using Persistence.LL;
using RealTime.LL;
using Services.AdminDashboard;
using Services.LL;

var builder = WebApplication.CreateBuilder(args);
var liveOpsRoot = builder.Environment.ContentRootPath;
var apiLLPath = Path.GetFullPath(Path.Combine(liveOpsRoot, "..", "API.LL"));

builder.Configuration
    .AddJsonFile(Path.Combine(apiLLPath, "appsettings.json"), optional: false, reloadOnChange: true)
    .AddJsonFile(
        Path.Combine(apiLLPath, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: true)
    .AddJsonFile(Path.Combine(liveOpsRoot, "appsettings.json"), optional: false, reloadOnChange: true)
    .AddJsonFile(
        Path.Combine(liveOpsRoot, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
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
builder.Services.AddApplication();
builder.Services.AddServices(
    config,
    apiLLPath,
    builder.Environment.IsDevelopment());
builder.Services.AddRealTime();
builder.Services.AddAdminDashboardServices();
builder.Services.AddCommonServices();

builder.Services.Configure<ChatModerationOptions>(
    config.GetSection(ChatModerationOptions.SectionName));
builder.Services.AddHttpClient<IChatModerationGateway, ChatModerationGateway>();

var allowedOrigins = config.GetSection("LiveOps:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy(
        "LiveOpsOrigins",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
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
app.MapControllers();
app.MapHealthChecks("/healthz/ready").AllowAnonymous();
app.MapHealthChecks("/healthz/live").AllowAnonymous();
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

    var chatBaseUrl = configuration["Chat:Moderation:BaseUrl"];
    var chatSecret = configuration["Chat:Moderation:Secret"];
    if (!Uri.TryCreate(chatBaseUrl, UriKind.Absolute, out _) ||
        string.IsNullOrWhiteSpace(chatSecret))
    {
        throw new InvalidOperationException(
            "Chat moderation base URL and secret are required outside Development.");
    }
}
