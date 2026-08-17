using System.Text.Json;
using System.Security.Claims;
using Application.UseCases.Administration;
using Microsoft.AspNetCore.Authorization;

namespace API.LiveOps.Authorization;

public static class LiveOpsAuthorization
{
    private static readonly string[] Permissions =
    [
        AdministrationPermissions.Read,
        AdministrationPermissions.AccountModeration,
        AdministrationPermissions.ChatModeration,
        AdministrationPermissions.EconomyCompensation,
        AdministrationPermissions.SuperAdmin
    ];

    public static void AddLiveOpsPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions)
        {
            options.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    HasPermission(context.User, permission) ||
                    HasPermission(context.User, AdministrationPermissions.SuperAdmin)));
        }
    }

    private static bool HasPermission(ClaimsPrincipal principal, string permission) =>
        principal.Claims
            .Where(claim => claim.Type is "permission" or "permissions" or "scope")
            .SelectMany(claim => ExpandClaimValue(claim.Value))
            .Any(value => string.Equals(
                value,
                permission,
                StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> ExpandClaimValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('['))
        {
            JsonDocument? document = null;
            try
            {
                document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement
                        .EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()!)
                        .ToArray();
                }
            }
            catch (JsonException)
            {
                // Invalid JSON is treated as a regular delimited claim below.
            }
            finally
            {
                document?.Dispose();
            }
        }

        return trimmed.Split(
            [' ', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
