using System.Security.Claims;
using System.Text.Json.Serialization;
using API.LiveOps.Authorization;
using API.LiveOps.Chat;
using Application;
using Application.Interfaces.Services.LL.Administration;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Persistence.LL;
using RealTime.LL;
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
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices(
    config,
    apiLLPath,
    builder.Environment.IsDevelopment());
builder.Services.AddRealTime();
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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = staffAuthority;
        options.Audience = staffAudience;
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = "name",
            RoleClaimType = "roles",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
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

if (allowedOrigins.Length > 0)
{
    app.UseCors("LiveOpsOrigins");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz/ready").AllowAnonymous();
app.MapHealthChecks("/healthz/live").AllowAnonymous();

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

    var chatBaseUrl = configuration["Chat:Moderation:BaseUrl"];
    var chatSecret = configuration["Chat:Moderation:Secret"];
    if (!Uri.TryCreate(chatBaseUrl, UriKind.Absolute, out _) ||
        string.IsNullOrWhiteSpace(chatSecret))
    {
        throw new InvalidOperationException(
            "Chat moderation base URL and secret are required outside Development.");
    }
}
