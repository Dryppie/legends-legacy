using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace API.LiveOps.Hosting;

public static class LiveOpsReverseProxy
{
    public const string SectionName = "ReverseProxy";

    public static void AddLiveOpsForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        if (!section.GetValue<bool>("Enabled"))
        {
            return;
        }

        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];
        var allowedHosts = (configuration["AllowedHosts"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = Math.Clamp(
                section.GetValue<int?>("ForwardLimit") ?? 1,
                1,
                5);
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            options.AllowedHosts.Clear();
            foreach (var host in allowedHosts)
            {
                options.AllowedHosts.Add(host);
            }
        });
    }
}
