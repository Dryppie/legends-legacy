using API.LL.Common;
using API.LL.Controllers.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

public sealed class MarketPlaceAuthorizationTests
{
    [Fact]
    public void Controller_RequiresRegisteredUserPolicy()
    {
        var authorizeAttribute = typeof(MarketPlaceController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy is not null);

        Assert.Equal(AuthorizationPolicies.RegisteredUser, authorizeAttribute.Policy);
    }

    [Theory]
    [InlineData("True", false)]
    [InlineData("False", true)]
    [InlineData(null, false)]
    public async Task RegisteredUserPolicy_OnlyAllowsExplicitNonGuestClaim(
        string? guestClaim,
        bool expectedAuthorization)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);

        await using var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        IEnumerable<Claim> claims = guestClaim is null
            ? []
            : [new Claim("guest", guestClaim)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            resource: null,
            AuthorizationPolicies.RegisteredUser);

        Assert.Equal(expectedAuthorization, result.Succeeded);
    }
}
