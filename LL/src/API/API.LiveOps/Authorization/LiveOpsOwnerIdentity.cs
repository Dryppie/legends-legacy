using System.Security.Claims;
using Application.UseCases.Administration;

namespace API.LiveOps.Authorization;

public static class LiveOpsOwnerIdentity
{
    public static bool TryGrantOwnerPermission(
        ClaimsPrincipal principal,
        string? ownerSubject,
        string? bootstrapOwnerEmail)
    {
        if (!IsOwner(principal, ownerSubject, bootstrapOwnerEmail) ||
            principal.Identity is not ClaimsIdentity identity)
        {
            return false;
        }

        if (!LiveOpsAuthorization.HasPermission(
                principal,
                AdministrationPermissions.SuperAdmin))
        {
            identity.AddClaim(new Claim(
                "permission",
                AdministrationPermissions.SuperAdmin));
        }

        return true;
    }

    public static bool IsOwner(
        ClaimsPrincipal principal,
        string? ownerSubject,
        string? bootstrapOwnerEmail)
    {
        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(ownerSubject))
        {
            return string.Equals(
                subject,
                ownerSubject.Trim(),
                StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(bootstrapOwnerEmail) ||
            !IsEmailVerified(principal))
        {
            return false;
        }

        var email = principal.FindFirstValue("email")
            ?? principal.FindFirstValue(ClaimTypes.Email);
        return string.Equals(
            email,
            bootstrapOwnerEmail.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmailVerified(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("email_verified");
        return bool.TryParse(value, out var verified) && verified;
    }
}
