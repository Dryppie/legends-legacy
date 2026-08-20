using API.LL;
using API.LL.Common;
using API.LL.Benchmarking;
using API.LL.HostedServices;
using Application;
using Application.Interfaces.Services.LL;
using Asp.Versioning;
using Common;
using Domain.Models.Users;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.LL;
using Persistence.LL.Seeds;
using RealTime.LL;
using Services.AdminDashboard;
using Services.LL;
using Services.LL.Administration;
using Services.LL.Validation;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var idleCombatBenchmark = config
    .GetSection(IdleCombatBenchmarkOptions.SectionName)
    .Get<IdleCombatBenchmarkOptions>() ?? new IdleCombatBenchmarkOptions();

if (idleCombatBenchmark.Enabled)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Idle-combat benchmark mode can only run in the Development environment.");
    }

    if (idleCombatBenchmark.FixedUtcNow is null)
    {
        throw new InvalidOperationException(
            "Idle-combat benchmark mode requires Benchmarking:IdleCombat:FixedUtcNow.");
    }
}

config
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<RefreshTokenRotationCoordinator>();
// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["requestId"] =
            RequestLoggingMiddleware.GetRequestId(context.HttpContext);
    };
});
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();

var signalR = builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var signalRRedis = config.GetConnectionString("Redis");
var useRedisSignalR = config.GetValue<bool>("SignalR:UseRedisBackplane");
if (useRedisSignalR && string.IsNullOrWhiteSpace(signalRRedis))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Redis is required when SignalR:UseRedisBackplane is enabled.");
}
if (useRedisSignalR)
{
    signalR.AddStackExchangeRedis(signalRRedis!, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("legends-legacy:game");
    });
}

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<AccountRestrictionSnapshotHealthCheck>(
        "account_restriction_snapshot",
        tags: ["ready"]);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = false;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:4200", "https://dev.legends-legacy.com")
                          .AllowCredentials()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithExposedHeaders("X-LL-State-Revisions", RequestLoggingMiddleware.RequestIdHeaderName));
});

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices(config, builder.Environment.ContentRootPath, builder.Environment.IsDevelopment());
builder.Services.AddSingleton<IAuthorizationHandler, ActiveAccountAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, MultiplayerAllowedAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AccountRestrictionAuthorizationResultHandler>();
builder.Services.AddHostedService<AccountRestrictionRefreshWorker>();
if (idleCombatBenchmark.Enabled)
{
    builder.Services.AddSingleton<TimeProvider>(
        new FixedTimeProvider(idleCombatBenchmark.FixedUtcNow!.Value));
}
else
{
    builder.Services.AddHostedService<GameEventOutboxWorker>();
    builder.Services.AddHostedService<DungeonPowerCalibrationWorker>();
    builder.Services.AddHostedService<RaidPowerCalibrationWorker>();
    builder.Services.AddHostedService<WorldTowerCombatSimulationWorker>();
    builder.Services.AddHostedService<WorldTowerCombatPlaybackWorker>();
    builder.Services.AddHostedService<RaidResolutionWorker>();
    builder.Services.AddHostedService<ChampionMarketTitleBackfillWorker>();
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHostedService<TournamentGroundsDevelopmentProgressionWorker>();
    }
}
builder.Services.AddRealTime(); // RealTime services must be added after Application and Persistence, as they depend on them
builder.Services.AddAdminDashboardServices(); // TODO: Application layer makes use of AdminDashboard services, so this is necessary at the moment.
                                              // At some point the application layer should perhaps be split up into two? One for LL, another for Dashboard
builder.Services.AddCommonServices();
builder.Services.SetupApi();
builder.Services.SetupSwagger("Legends Legacy", config);

// TODO: Make an extension method
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.UserData
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
#if DEBUG
                // Check Authorization against DevAuth header from Swagger
                var authHeader = ctx.Request.Headers["DevAuth"].FirstOrDefault();
                if (authHeader != null)
                {
                    ctx.Token = ctx.Request.Headers["DevAuth"].FirstOrDefault();
                }
#endif
                var accessToken = ctx.Request.Query["access_token"].FirstOrDefault();
                var path = ctx.HttpContext.Request.Path;

                if (string.IsNullOrEmpty(ctx.Token)
                    && !string.IsNullOrWhiteSpace(accessToken)
                    && path.StartsWithSegments("/hub/game"))
                {
                    ctx.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var isAllowAnonymous = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
                if (isAllowAnonymous)
                {
                    return Task.CompletedTask;
                }

                var hasUserId = Guid.TryParse(
                    context.Principal?.FindFirstValue(ClaimTypes.UserData),
                    out _);
                var hasCharacterId = context.Principal?.FindFirstValue("CharacterId") is not null;

                if (!hasUserId || !hasCharacterId)
                {
                    context.Fail("The access token is missing required identity claims.");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // If value is not null the endpoint is decorated with the [AllowAnonymous] attribute
                var isAllowAnonymous = context?.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

                var excType = context?.Exception.GetType();

                var isTokenExpired = excType == typeof(SecurityTokenExpiredException);
                var signatureRotated = excType == typeof(SecurityTokenSignatureKeyNotFoundException);

                if (!isAllowAnonymous && (isTokenExpired || signatureRotated))
                {
                    context?.Response.Headers.Append("invalid_access_token", "The access token provided is expired, revoked, malformed, or invalid for other reasons");
                }

                return Task.CompletedTask;
            },
        };
    }
);
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LLDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

    // Migrate and Seed
        await context.Database.MigrateAsync();
    var seedLocalGuestAccounts = config.GetValue<bool>("FeatureManagement:SeedLocalGuestAccounts");
    await context.SeedData(hasher, seedLocalGuestAccounts);
    await scope.ServiceProvider.GetRequiredService<AccountRestrictionIndex>()
        .RefreshAsync(
            scope.ServiceProvider.GetRequiredService<IAdministrationRepository>(),
            CancellationToken.None);
}

await app.Services.ValidateCreatureBuildProfilesAsync();

// Configure the HTTP request pipeline.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");

if (config.GetValue<bool>("FeatureManagement:DisableAllRequests"))
{
    var maintenanceMessage = config.GetValue<string>("FeatureManagement:MaintenanceMessage")
        ?? "The game is currently undergoing maintenance.";
    var retryAfterSeconds = config.GetValue<int?>("FeatureManagement:MaintenanceRetryAfterSeconds") ?? 300;

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/healthz/ready") || path.StartsWithSegments("/healthz/live"))
        {
            await next();
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        await context.Response.WriteAsync(maintenanceMessage);
    });
}

if (!app.Environment.IsDevelopment())       // prod only
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<AuthenticatedIdentityLoggingMiddleware>();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    var stateSync = context.RequestServices.GetRequiredService<IStateSyncService>();
    context.Response.OnStarting(() =>
    {
        var characterId = Guid.TryParse(
            context.User.FindFirstValue("CharacterId"),
            out var parsedCharacterId)
            ? parsedCharacterId
            : (Guid?)null;
        var changedRevisions = stateSync.GetChangedRevisions(characterId);
        if (changedRevisions.Count > 0)
        {
            context.Response.Headers["X-LL-State-Revisions"] =
                System.Text.Json.JsonSerializer.Serialize(changedRevisions);
        }
        return Task.CompletedTask;
    });
    await next();
});

app.MapHub<GameHub>("/hub/game").RequireAuthorization();

if (app.Environment.IsDevelopment() && config.GetValue("FeatureManagement:AllowAnonymous", "false") == "true")
{
    app.MapControllers().AllowAnonymous();
}
else
{
    app.MapControllers();
}

app.MapHealthChecks("/healthz/ready").AllowAnonymous();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.Run();
