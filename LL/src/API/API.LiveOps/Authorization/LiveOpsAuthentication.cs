using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace API.LiveOps.Authorization;

public static class LiveOpsAuthentication
{
    public const string DynamicScheme = "LiveOps";
    public const string CookieScheme = "LiveOpsCookie";
    public const string OidcScheme = "LiveOpsOidc";
    private const string AbsoluteExpiryProperty = ".liveops.absolute-expires";

    public static AuthenticationBuilder AddLiveOpsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authority = configuration["StaffIdentity:Authority"] ?? string.Empty;
        var audience = configuration["StaffIdentity:Audience"] ?? string.Empty;
        var clientId = configuration["StaffIdentity:ClientId"] ?? "liveops-dashboard";
        var clientSecret = configuration["StaffIdentity:ClientSecret"] ?? string.Empty;
        var ownerSubject = configuration["StaffIdentity:OwnerSubject"];
        var bootstrapOwnerEmail = configuration["StaffIdentity:BootstrapOwnerEmail"];

        return services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = DynamicScheme;
                options.DefaultAuthenticateScheme = DynamicScheme;
                options.DefaultChallengeScheme = DynamicScheme;
            })
            .AddPolicyScheme(DynamicScheme, DynamicScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.Authorization.ToString()
                        .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieScheme;
            })
            .AddCookie(CookieScheme, options =>
            {
                options.Cookie.Name = environment.IsDevelopment()
                    ? "LL-LiveOps"
                    : "__Host-LL-LiveOps";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.LoginPath = "/auth/login";
                options.AccessDeniedPath = "/access-denied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnSigningIn = context =>
                    {
                        if (!context.Properties.Items.ContainsKey(AbsoluteExpiryProperty))
                        {
                            context.Properties.Items[AbsoluteExpiryProperty] =
                                DateTimeOffset.UtcNow.AddHours(8).ToString("O");
                        }

                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = context =>
                    {
                        var hasValidExpiry = context.Properties.Items.TryGetValue(
                            AbsoluteExpiryProperty,
                            out var value) &&
                            DateTimeOffset.TryParse(value, out var absoluteExpiry) &&
                            absoluteExpiry > DateTimeOffset.UtcNow;
                        if (!hasValidExpiry)
                        {
                            context.RejectPrincipal();
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToLogin = context =>
                        HandleApiRedirect(context, StatusCodes.Status401Unauthorized),
                    OnRedirectToAccessDenied = context =>
                        HandleApiRedirect(context, StatusCodes.Status403Forbidden)
                };
            })
            .AddOpenIdConnect(OidcScheme, options =>
            {
                options.SignInScheme = CookieScheme;
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.CallbackPath = configuration["StaffIdentity:CallbackPath"]
                    ?? "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    NameClaimType = "name",
                    RoleClaimType = "roles",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Scope.Clear();
                foreach (var scope in configuration
                             .GetSection("StaffIdentity:Scopes")
                             .Get<string[]>() ?? ["openid", "profile", "email"])
                {
                    options.Scope.Add(scope);
                }
                options.ClaimActions.MapUniqueJsonKey("permission", "permission");
                options.ClaimActions.MapUniqueJsonKey("permissions", "permissions");
                options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
                options.ClaimActions.MapUniqueJsonKey("email_verified", "email_verified");
                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        if (context.Principal is not null &&
                            LiveOpsOwnerIdentity.TryGrantOwnerPermission(
                                context.Principal,
                                ownerSubject,
                                bootstrapOwnerEmail))
                        {
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect("/?authentication=denied");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    NameClaimType = "name",
                    RoleClaimType = "roles",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
    }

    private static Task HandleApiRedirect(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/auth/session") ||
            context.Request.Path.StartsWithSegments("/auth/antiforgery"))
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
