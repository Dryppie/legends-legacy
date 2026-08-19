using Domain.Models.Administration;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed class AccountRestrictionIndexTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Most_restrictive_active_account_state_wins()
    {
        var accountId = Guid.NewGuid();
        var multiplayer = Restriction(
            accountId,
            AccountRestrictionType.MultiplayerRestriction,
            Now.AddHours(1));

        var multiplayerAccess = AccountAccessSnapshot.From([multiplayer], Now);
        Assert.True(multiplayerAccess.CanAuthenticate);
        Assert.False(multiplayerAccess.CanParticipate);
        Assert.False(multiplayerAccess.IsPubliclyEligible);

        var ban = Restriction(accountId, AccountRestrictionType.Ban, null);
        var bannedAccess = AccountAccessSnapshot.From([multiplayer, ban], Now);
        Assert.False(bannedAccess.CanAuthenticate);
        Assert.Same(ban, bannedAccess.EffectiveRestriction);
    }

    [Fact]
    public void Lookup_evaluates_expiry_without_waiting_for_an_index_refresh()
    {
        var time = new MutableTimeProvider { Now = Now };
        var index = new AccountRestrictionIndex(time);
        var accountId = Guid.NewGuid();
        index.Replace(
            [Restriction(accountId, AccountRestrictionType.MultiplayerRestriction, Now.AddMinutes(5))],
            Now);

        Assert.False(index.Get(accountId).CanParticipate);

        time.Now = Now.AddMinutes(6);

        Assert.True(index.Get(accountId).CanParticipate);
        Assert.Equal(Now, index.RefreshedAt);
    }

    [Fact]
    public void Revoked_restrictions_are_ignored_during_access_derivation()
    {
        var restriction = Restriction(
            Guid.NewGuid(),
            AccountRestrictionType.MultiplayerRestriction,
            null);
        restriction.Revoke("staff|moderator", "Appeal approved.", Now);

        var access = AccountAccessSnapshot.From([restriction], Now.AddSeconds(1));

        Assert.Equal(AccountAccessSnapshot.Unrestricted, access);
    }

    private static AccountRestriction Restriction(
        Guid accountId,
        AccountRestrictionType type,
        DateTimeOffset? expiresAt) => new()
    {
        AccountId = accountId,
        RestrictionType = type,
        Reason = "Test restriction",
        CreatedBySubject = "staff|moderator",
        CreatedAt = Now,
        ExpiresAt = expiresAt
    };

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
