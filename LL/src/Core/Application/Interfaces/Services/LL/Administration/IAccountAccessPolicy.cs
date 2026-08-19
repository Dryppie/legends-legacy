using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface IAccountAccessPolicy
{
    Task<AccountAccessSnapshot> GetAccessAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<AccountRestriction?> GetActiveBanAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}
