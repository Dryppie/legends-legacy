using System.Security.Claims;
using API.LiveOps.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace EssenceSystem.Tests;

public sealed class LiveOpsDataProtectionTests
{
    [Fact]
    public async Task Session_and_antiforgery_tokens_survive_restart_with_a_different_content_root()
    {
        var directory = Directory.CreateTempSubdirectory("liveops-keys-");
        try
        {
            string sessionCookie;
            string antiforgeryCookie;
            string requestToken;
            using (var first = CreateServices(directory.FullName, "Production", "first-content-root"))
            {
                var context = new DefaultHttpContext { RequestServices = first, User = Operator() };
                var tokens = first.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
                requestToken = tokens.RequestToken!;
                antiforgeryCookie = context.Response.Headers.SetCookie.ToString().Split(';')[0];
                sessionCookie = TicketFormat(first).Protect(
                    new AuthenticationTicket(Operator(), new AuthenticationProperties(), "LiveOpsCookie"));
            }

            using var restarted = CreateServices(directory.FullName, "Production", "new-content-root");
            var ticket = TicketFormat(restarted).Unprotect(sessionCookie);
            Assert.NotNull(ticket);
            Assert.Equal("operator-123", ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var request = new DefaultHttpContext { RequestServices = restarted, User = ticket.Principal };
            request.Request.Method = "POST";
            request.Request.Headers.Cookie = antiforgeryCookie;
            request.Request.Headers["X-XSRF-TOKEN"] = requestToken;
            await restarted.GetRequiredService<IAntiforgery>().ValidateRequestAsync(request);
            Assert.NotEmpty(directory.GetFiles("key-*.xml"));

            // Even if operators accidentally mount the same claim, environments stay isolated.
            using var staging = CreateServices(directory.FullName, "Staging", "new-content-root");
            Assert.Null(TicketFormat(staging).Unprotect(sessionCookie));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("Production", null)]
    [InlineData("Staging", " ")]
    [InlineData("Production", "relative/keys")]
    [InlineData("Development", "relative/keys")]
    public void Missing_or_relative_persistent_paths_are_rejected(string environment, string? path)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateServices(path, environment, "root"));
        Assert.Contains(LiveOpsDataProtection.KeyRingPathConfigurationKey, exception.Message);
    }

    [Fact]
    public void Development_can_use_default_local_storage()
    {
        using var services = CreateServices(null, "Development", "root");
        Assert.NotNull(services.GetRequiredService<IDataProtectionProvider>());
    }

    private static TicketDataFormat TicketFormat(IServiceProvider services) => new(
        services.GetRequiredService<IDataProtectionProvider>().CreateProtector(
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
            "LiveOpsCookie", "v2"));

    private static ClaimsPrincipal Operator() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "operator-123")], "LiveOpsCookie"));

    private static ServiceProvider CreateServices(string? path, string environment, string contentRoot)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [LiveOpsDataProtection.KeyRingPathConfigurationKey] = path }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection().SetApplicationName(contentRoot);
        services.AddLiveOpsDataProtection(configuration, new TestEnvironment(environment));
        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "LL-LiveOps-XSRF";
            options.HeaderName = "X-XSRF-TOKEN";
        });
        return services.BuildServiceProvider();
    }

    private sealed class TestEnvironment(string name) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "API.LiveOps";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
