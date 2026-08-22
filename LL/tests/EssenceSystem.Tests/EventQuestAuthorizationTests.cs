using System.Reflection;
using API.LL.Controllers.V1;
using Microsoft.AspNetCore.Authorization;

namespace EssenceSystem.Tests;

public sealed class EventQuestAuthorizationTests
{
    [Fact]
    public void Controller_allows_authenticated_guest_accounts()
    {
        var authorizeAttributes = typeof(EventQuestController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        Assert.NotEmpty(authorizeAttributes);
        Assert.All(authorizeAttributes, attribute => Assert.Null(attribute.Policy));
    }
}
