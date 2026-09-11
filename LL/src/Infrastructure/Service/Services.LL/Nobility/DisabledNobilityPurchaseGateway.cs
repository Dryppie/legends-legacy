using Application.Interfaces.Services.LL.Nobility;

namespace Services.LL.Nobility;

public sealed class DisabledNobilityPurchaseGateway : INobilityPurchaseGateway
{
    public Task<NobilityCheckoutResult> CreateCheckoutAsync(Guid accountId, Guid characterId, string productId, CancellationToken ct) =>
        Task.FromResult(new NobilityCheckoutResult(false, null, "Purchases are unavailable during alpha. Signets are distributed by the game team and can be traded on the market."));
}
