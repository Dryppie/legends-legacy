using System.Text.Json.Serialization;
using Application;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Persistence.LL;
using Services.AdminDashboard;
using Services.LL;
using Services.LL.Validation;
using RealTime.LL;
using System.Net;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var currentDirectory = Directory.GetCurrentDirectory(); // API.AdminDashboard
var apiDirectory = Directory.GetParent(currentDirectory)!.FullName; // API folder

var apiLLPath = Path.Combine(apiDirectory, "API.LL"); // Full path to API.LL

config
    .SetBasePath(apiLLPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:4300")
                          .AllowCredentials()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices(config, apiLLPath, builder.Environment.IsDevelopment());
builder.Services.AddRealTime();
builder.Services.AddAdminDashboardServices();
builder.Services.AddCommonServices();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtIssuer = config["Jwt:Issuer"];
        var jwtAudience = config["Jwt:Audience"];

        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!)),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.UserData
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminDashboard", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var remoteAddress = context.Resource switch
            {
                HttpContext httpContext => httpContext.Connection.RemoteIpAddress,
                Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext filterContext =>
                    filterContext.HttpContext.Connection.RemoteIpAddress,
                _ => null
            };

            return remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);
        });
    });

var app = builder.Build();

await app.Services.ValidateCreatureBuildProfilesAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
