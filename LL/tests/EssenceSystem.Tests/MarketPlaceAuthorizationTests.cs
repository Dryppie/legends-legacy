using API.LL.Common;
using API.LL.Controllers.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Services.LL.Administration;
using Application.Interfaces.Services.LL.Administration;

public sealed class MarketPlaceAuthorizationTests
{
    [Theory]
    [InlineData(typeof(InventoryController), nameof(InventoryController.Transfer))]
    [InlineData(typeof(CharacterController), nameof(CharacterController.Wire))]
    public void Direct_player_transfer_endpoints_require_multiplayer_policy(
        Type controllerType,
        string methodName)
    {
        var method = controllerType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Controller method '{methodName}' was not found.");
        var authorizeAttribute = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.Equal(AuthorizationPolicies.MultiplayerAllowed, authorizeAttribute.Policy);
    }

    [Fact]
    public void Controller_requires_multiplayer_policy()
    {
        var authorizeAttribute = typeof(MarketPlaceController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.Equal(AuthorizationPolicies.MultiplayerAllowed, authorizeAttribute.Policy);
    }

    [Theory]
    [InlineData("True", true, false)]
    [InlineData("False", true, true)]
    [InlineData(null, true, false)]
    [InlineData("False", false, false)]
    public async Task RegisteredUserPolicy_OnlyAllowsExplicitNonGuestClaim(
        string? guestClaim,
        bool isAuthenticated,
        bool expectedAuthorization)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddSingleton<IAccountRestrictionIndex>(
            new AccountRestrictionIndex(TimeProvider.System));
        services.AddSingleton<IAuthorizationHandler, ActiveAccountAuthorizationHandler>();

        await using var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, Guid.NewGuid().ToString())
        };
        if (guestClaim is not null)
        {
            claims.Add(new Claim("guest", guestClaim));
        }
        var authenticationType = isAuthenticated ? "Test" : null;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            resource: null,
            AuthorizationPolicies.RegisteredUser);

        Assert.Equal(expectedAuthorization, result.Succeeded);
    }
}
