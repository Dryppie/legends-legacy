using Application;
using Application.Interfaces.WebSockets;
using Common;
using Persistence.LL;
using Services.AdminDashboard;
using Services.LL;
using Worker.LL.BackgroundJobs;
using Worker.LL.Realtime;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

config
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddPersistence(config);
builder.Services.AddRepositories();
builder.Services.AddApplication();
builder.Services.AddCommonServices();
builder.Services.AddScoped<IGameEventPublisher, NoOpGameEventPublisher>();
builder.Services.AddScoped<IGameRealtimeBroadcaster, NoOpGameRealtimeBroadcaster>();
builder.Services.AddServices(config, builder.Environment.ContentRootPath);
builder.Services.AddAdminDashboardServices();
builder.Services.AddBackgroundJobInfrastructure(config, builder.Environment);

var host = builder.Build();
await host.RunAsync();
