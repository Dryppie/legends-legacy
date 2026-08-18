using API.LiveOps.Controllers;
using Application.UseCases.Administration;
using Microsoft.AspNetCore.Authorization;

namespace EssenceSystem.Tests;

public sealed class AccountRiskAuthorizationTests
{
    [Theory]
    [InlineData(nameof(AccountRiskController.Search))]
    [InlineData(nameof(AccountRiskController.GetDetails))]
    public void Read_endpoints_require_liveops_read_permission(string methodName)
    {
        AssertPolicy(methodName, AdministrationPermissions.Read);
    }

    [Theory]
    [InlineData(nameof(AccountRiskController.UpdateStatus))]
    [InlineData(nameof(AccountRiskController.AddNote))]
    public void Investigation_mutations_require_account_moderation_permission(string methodName)
    {
        AssertPolicy(methodName, AdministrationPermissions.AccountModeration);
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(AccountRiskController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Controller method '{methodName}' was not found.");
        var authorization = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.Equal(expectedPolicy, authorization.Policy);
    }
}
