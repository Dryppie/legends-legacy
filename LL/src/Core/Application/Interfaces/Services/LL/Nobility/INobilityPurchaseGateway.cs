namespace Application.Interfaces.Services.LL.Nobility;

// Future Stripe integration creates checkout sessions here. Fulfilment must issue audited Signet units.
public interface INobilityPurchaseGateway
{
    Task<NobilityCheckoutResult> CreateCheckoutAsync(Guid accountId, Guid characterId, string productId, CancellationToken ct);
}

public sealed record NobilityCheckoutResult(bool Available, string? CheckoutUrl, string Message);
