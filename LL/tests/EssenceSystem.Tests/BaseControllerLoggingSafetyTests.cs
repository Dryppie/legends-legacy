using System.Security.Claims;
using API.LL.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssenceSystem.Tests;

public sealed class BaseControllerLoggingSafetyTests
{
    [Fact]
    public void Missing_identity_claim_does_not_copy_other_claims_into_exception()
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Email, "private@example.test"),
                        new Claim(ClaimTypes.Name, "Private Character Name")
                    ], "test"))
                }
            }
        };

        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => controller.GetCurrentAccountId());

        Assert.Equal("User ID claim is missing.", exception.Message);
        Assert.DoesNotContain("private@example.test", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Private Character Name", exception.Message, StringComparison.Ordinal);
    }

    private sealed class TestController : BaseController
    {
        public Guid GetCurrentAccountId() => CurrentUserId;
    }
}
