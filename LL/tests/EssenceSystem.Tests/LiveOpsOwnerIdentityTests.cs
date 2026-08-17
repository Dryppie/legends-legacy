using System.Security.Claims;
using API.LiveOps.Authorization;
using Application.UseCases.Administration;

namespace EssenceSystem.Tests;

public sealed class LiveOpsOwnerIdentityTests
{
    [Fact]
    public void Matching_subject_receives_superadmin_permission()
    {
        var principal = Principal(
            new Claim("sub", "google-owner-123"),
            new Claim("email", "owner@example.test"),
            new Claim("email_verified", "true"));

        var granted = LiveOpsOwnerIdentity.TryGrantOwnerPermission(
            principal,
            "google-owner-123",
            null);

        Assert.True(granted);
        Assert.True(LiveOpsAuthorization.HasPermission(
            principal,
            AdministrationPermissions.SuperAdmin));
    }

    [Fact]
    public void Configured_subject_takes_precedence_over_matching_email()
    {
        var principal = Principal(
            new Claim("sub", "different-google-user"),
            new Claim("email", "owner@example.test"),
            new Claim("email_verified", "true"));

        var granted = LiveOpsOwnerIdentity.TryGrantOwnerPermission(
            principal,
            "google-owner-123",
            "owner@example.test");

        Assert.False(granted);
    }

    [Fact]
    public void Verified_email_can_bootstrap_the_initial_owner()
    {
        var principal = Principal(
            new Claim("sub", "google-owner-123"),
            new Claim("email", "Owner@Example.Test"),
            new Claim("email_verified", "true"));

        var granted = LiveOpsOwnerIdentity.TryGrantOwnerPermission(
            principal,
            null,
            "owner@example.test");

        Assert.True(granted);
    }

    [Fact]
    public void Unverified_email_cannot_bootstrap_the_owner()
    {
        var principal = Principal(
            new Claim("sub", "google-owner-123"),
            new Claim("email", "owner@example.test"),
            new Claim("email_verified", "false"));

        var granted = LiveOpsOwnerIdentity.TryGrantOwnerPermission(
            principal,
            null,
            "owner@example.test");

        Assert.False(granted);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "oidc"));
}
