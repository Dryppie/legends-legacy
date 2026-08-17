using System.Net;
using System.Security.Claims;
using API.LiveOps.Authorization;
using Application.UseCases.Administration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl = "/")
    {
        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(safeReturnUrl);
        }

        if (CanUseDevelopmentOperator())
        {
            var claims = new[]
            {
                new Claim("sub", "development-operator"),
                new Claim("name", "Local Development Operator"),
                new Claim("preferred_username", "local-operator"),
                new Claim("permission", AdministrationPermissions.SuperAdmin)
            };
            var identity = new ClaimsIdentity(
                claims,
                LiveOpsAuthentication.CookieScheme,
                "name",
                "roles");
            await HttpContext.SignInAsync(
                LiveOpsAuthentication.CookieScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                });
            return LocalRedirect(safeReturnUrl);
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = safeReturnUrl },
            LiveOpsAuthentication.OidcScheme);
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        return CanUseDevelopmentOperator()
            ? SignOut(properties, LiveOpsAuthentication.CookieScheme)
            : SignOut(
                properties,
                LiveOpsAuthentication.CookieScheme,
                LiveOpsAuthentication.OidcScheme);
    }

    [Authorize]
    [HttpGet("session")]
    public IActionResult Session() => Ok(new
    {
        subject = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        displayName = User.FindFirstValue("name")
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name,
        permissions = LiveOpsAuthorization.GetPermissions(User),
        environment = environment.EnvironmentName,
        isDevelopmentOperator = string.Equals(
            User.FindFirstValue("sub"),
            "development-operator",
            StringComparison.Ordinal)
    });

    [Authorize]
    [HttpGet("antiforgery")]
    public IActionResult AntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    private bool CanUseDevelopmentOperator()
    {
        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        return environment.IsDevelopment() &&
               configuration.GetValue<bool>("LiveOps:DevelopmentOperator:Enabled") &&
               remoteAddress is not null &&
               IPAddress.IsLoopback(remoteAddress);
    }
}
