using Domain.Models.Prophecies;

namespace Application.Interfaces.Services.LL.Prophecies;

public interface IProphecyBalanceProvider
{
    ProphecyBalanceCatalog GetCatalog();
}
