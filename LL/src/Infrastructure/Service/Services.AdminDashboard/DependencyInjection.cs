using Application.Interfaces.Services.AdminDashboard;
using Microsoft.Extensions.DependencyInjection;
using Services.AdminDashboard.Creatures;

namespace Services.AdminDashboard;
public static class DependencyInjection
{
    public static IServiceCollection AddAdminDashboardServices(this IServiceCollection services)
    {

        services.AddScoped<ICreatureService, CreatureService>();
        

        return services;
    }
}