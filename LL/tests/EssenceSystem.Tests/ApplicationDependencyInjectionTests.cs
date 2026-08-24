using Application;
using Application.UseCases._AdminDashboard.Items.Queries.GetItemBases;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_ExcludesAdminDashboardHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(GetItemBasesQueryHandler));
    }

    [Fact]
    public void AddAdminDashboardApplication_IncludesAdminDashboardHandlers()
    {
        var services = new ServiceCollection();

        services.AddAdminDashboardApplication();

        Assert.Contains(
            services,
            descriptor => descriptor.ImplementationType == typeof(GetItemBasesQueryHandler));
    }
}
