using Application;
using Common;
using Persistence.LL;
using Services.AdminDashboard;
using Services.LL;
using Worker.LL.BackgroundJobs;

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
builder.Services.AddServices(config, builder.Environment.ContentRootPath, builder.Environment.IsDevelopment());
builder.Services.AddAdminDashboardServices();
builder.Services.AddBackgroundJobInfrastructure(config, builder.Environment);

var host = builder.Build();
await host.RunAsync();
