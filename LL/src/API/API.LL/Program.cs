using API.LL;
using API.LL.HostedServices;
using Application;
using Asp.Versioning;
using Common;
using Domain.Models.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.LL;
using Persistence.LL.Seeds;
using RealTime.LL;
using Services.AdminDashboard;
using Services.LL;
using Services.LL.Validation;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

config
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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
                          .AllowAnyHeader());
});

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices(config, builder.Environment.ContentRootPath);
builder.Services.AddHostedService<TournamentProgressionHostedService>();
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
        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
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
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var isAllowAnonymous = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
                if (isAllowAnonymous)
                {
                    return Task.CompletedTask;
                }

                var hasUserId = context.Principal?.FindFirstValue(ClaimTypes.UserData) is not null;
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LLDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

    // Migrate and Seed
    await context.Database.MigrateAsync();
    await context.SeedData(hasher, app.Environment.IsDevelopment());
}

await app.Services.ValidateCreatureBuildProfilesAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");

if (config.GetValue("FeatureManagement:DisableAllRequests", "false") == "true")
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path != null && (path.Contains("/healthz/ready") || path.Contains("/healthz/live")))
        {
            await next(); // allow health checks through
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsync("The backend is currently unavailable.");
    });
}

if (!app.Environment.IsDevelopment())       // prod only
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

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
