using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountAccessPolicy(
    IAdministrationRepository administration,
    TimeProvider timeProvider) : IAccountAccessPolicy
{
    public async Task<AccountAccessSnapshot> GetAccessAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var ban = await administration.GetActiveRestrictionAsync(
            accountId,
            AccountRestrictionType.Ban,
            now,
            cancellationToken);
        if (ban is not null)
        {
            return AccountAccessSnapshot.From([ban], now);
        }

        var multiplayerRestriction = await administration.GetActiveRestrictionAsync(
            accountId,
            AccountRestrictionType.MultiplayerRestriction,
            now,
            cancellationToken);
        return multiplayerRestriction is null
            ? AccountAccessSnapshot.Unrestricted
            : AccountAccessSnapshot.From([multiplayerRestriction], now);
    }

    public Task<AccountRestriction?> GetActiveBanAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        administration.GetActiveAccountBanAsync(
            accountId,
            timeProvider.GetUtcNow(),
            cancellationToken);
}
