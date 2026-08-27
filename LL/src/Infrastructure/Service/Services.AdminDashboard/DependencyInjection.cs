using Application.Interfaces.Services.AdminDashboard;
using Microsoft.Extensions.DependencyInjection;
using Services.AdminDashboard.Combat;
using Services.AdminDashboard.Creatures;
using Services.AdminDashboard.Items;

namespace Services.AdminDashboard;
public static class DependencyInjection
{
    public static IServiceCollection AddAdminDashboardServices(this IServiceCollection services)
    {

        services.AddScoped<ICreatureService, CreatureService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddSingleton<WorldTowerAuditCampaignService>();
        services.AddSingleton<IWorldTowerAuditCampaignService>(serviceProvider =>
            serviceProvider.GetRequiredService<WorldTowerAuditCampaignService>());
        services.AddHostedService(serviceProvider =>
            serviceProvider.GetRequiredService<WorldTowerAuditCampaignService>());
        return services;
    }
}
