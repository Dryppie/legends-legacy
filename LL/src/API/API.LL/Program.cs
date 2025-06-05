using API.LL;
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
using Services.AdminDashboard;
using Services.LL;
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

// Authorization policy FallbackPolicy is applied globally. Overridden by [] Attributes on specific endpoints
//builder.Services.AddAuthorization(); // For Identity

// TODO: Apply policies during release
//builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

//builder.Services.AddIdentityApiEndpoints<AppUser>()
//    .AddEntityFrameworkStores<LLDbContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:4200", "https://dev.legends-legacy.com")
                          .AllowCredentials()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

//builder.Services.ConfigureApplicationCookie(options =>
//{
//    // Cookie settings
//    options.Cookie.HttpOnly = true;
//    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

//    options.LoginPath = "/Identity/Account/Login";
//    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
//    options.SlidingExpiration = true;
//});

// Dependency Injections
builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices();
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
            ValidateIssuer = false, // Needs to be true
            ValidateAudience = false, // Needs to be true
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
#if DEBUG
                // Check Authorization against DevAuth header from Swagger
                var authHeader = context.Request.Headers["DevAuth"].FirstOrDefault();
                if (authHeader != null)
                {
                    context.Token = context.Request.Headers["DevAuth"].FirstOrDefault();
                }
#endif
                var token = context.Token;

                // Add support for token origin to either be from a http-header or from a http-only cookie
                // https://alimozdemir.medium.com/asp-net-core-jwt-and-refresh-token-with-httponly-cookies-b1b96c849742
                if (context.Request.Cookies.ContainsKey("AccessToken") && context.Token is null)
                {
                    context.Token = context.Request.Cookies["AccessToken"];
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
    await context.SeedData(hasher);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

if (config.GetValue("FeatureManagement:AllowAnonymous", false))
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
