using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class AccountAccessPolicy(
    IAdministrationRepository administration,
    TimeProvider timeProvider) : IAccountAccessPolicy
{
    public Task<AccountRestriction?> GetActiveBanAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        administration.GetActiveAccountBanAsync(
            accountId,
            timeProvider.GetUtcNow(),
            cancellationToken);
}
