using Application;
using Application.MediatR.Behaviors;
using Application.UseCases._AdminDashboard.Items.Queries.GetItemBases;
using MediatR;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Application_pipeline_propagates_unexpected_exceptions_to_the_host_boundary(
        bool includeAdminDashboardHandlers)
    {
        var services = new ServiceCollection();

        if (includeAdminDashboardHandlers)
        {
            services.AddAdminDashboardApplication();
        }
        else
        {
            services.AddApplication();
        }

        var pipelineBehaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .ToArray();

        var applicationBehaviors = pipelineBehaviors
            .Select(descriptor => descriptor.ImplementationType!)
            .Where(type =>
                type is not null &&
                type.Assembly == typeof(TransactionBehavior<,>).Assembly)
            .ToArray();

        Assert.Equal([typeof(TransactionBehavior<,>)], applicationBehaviors);
    }
}
