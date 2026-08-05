using Domain.Models.Colosseum;

namespace Application.Interfaces.Services.LL.Colosseum;

public interface IChampionMarketCatalog
{
    IReadOnlyList<ChampionMarketItem> GetAll();
    IReadOnlyList<ChampionMarketItem> GetActive(DateTimeOffset now);
    ChampionMarketItem? GetById(string itemId);
}
