using Application;
using Common;
using Persistence.LL;
using Services.LL;
using Services.AdminDashboard;
using Domain.Models.Users;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var test = Directory.GetCurrentDirectory();

var currentDirectory = Directory.GetCurrentDirectory(); // API.AdminDashboard
var apiDirectory = Directory.GetParent(currentDirectory)!.FullName; // API folder

var apiLLPath = Path.Combine(apiDirectory, "API.LL"); // Full path to API.LL

config
    .SetBasePath(apiLLPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<LLDbContext>();

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

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddServices();
builder.Services.AddAdminDashboardServices();
builder.Services.AddCommonServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

app.MapControllers();

app.Run();
