namespace API.LiveOps.Hosting;

public static class LiveOpsPublicOrigin
{
    public const string ConfigurationKey = "LiveOps:PublicBaseUrl";

    public static IApplicationBuilder UseLiveOpsPublicOrigin(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        if (!TryParse(configuration[ConfigurationKey], out var publicBaseUri))
        {
            return app;
        }

        var publicHost = publicBaseUri.IsDefaultPort
            ? new HostString(publicBaseUri.Host)
            : new HostString(publicBaseUri.Host, publicBaseUri.Port);

        return app.Use(async (context, next) =>
        {
            context.Request.Scheme = publicBaseUri.Scheme;
            context.Request.Host = publicHost;
            await next(context);
        });
    }

    public static bool TryParse(string? value, out Uri publicBaseUri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            parsed.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            publicBaseUri = null!;
            return false;
        }

        publicBaseUri = parsed;
        return true;
    }
}
