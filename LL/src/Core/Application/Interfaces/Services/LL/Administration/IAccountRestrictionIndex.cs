using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface IAccountRestrictionIndex
{
    AccountAccessSnapshot Get(Guid accountId);
    DateTimeOffset? RefreshedAt { get; }
}
