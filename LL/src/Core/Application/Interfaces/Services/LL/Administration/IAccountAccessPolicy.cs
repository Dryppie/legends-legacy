using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface IAccountAccessPolicy
{
    Task<AccountRestriction?> GetActiveBanAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}
