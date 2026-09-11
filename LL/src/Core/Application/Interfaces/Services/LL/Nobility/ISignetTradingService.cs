using Domain.Models.MarketPlaces;

namespace Application.Interfaces.Services.LL.Nobility;

public interface ISignetTradingService
{
    Task ReserveAsync(MarketPlaceListing listing, CancellationToken ct);
    Task ReleaseAsync(MarketPlaceListing listing, CancellationToken ct);
    Task TradeAsync(MarketPlaceOrder trade, Guid? listingId, CancellationToken ct);
}
