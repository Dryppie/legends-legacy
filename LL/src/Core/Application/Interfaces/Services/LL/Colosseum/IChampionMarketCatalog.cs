using Domain.Models.Colosseum;

namespace Application.Interfaces.Services.LL.Colosseum;

public interface IChampionMarketCatalog
{
    IReadOnlyList<ChampionMarketItem> GetAll();
    ChampionMarketItem? GetById(string itemId);
}
