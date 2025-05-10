using Application.Interfaces.Services.AdminDashboard;
using Microsoft.Extensions.DependencyInjection;
using Services.AdminDashboard.Creatures;
using Services.AdminDashboard.Items;
using Services.AdminDashboard.Recipes;

namespace Services.AdminDashboard;
public static class DependencyInjection
{
    public static IServiceCollection AddAdminDashboardServices(this IServiceCollection services)
    {

        services.AddScoped<ICreatureService, CreatureService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IRecipeService, RecipeService>();

        return services;
    }
}