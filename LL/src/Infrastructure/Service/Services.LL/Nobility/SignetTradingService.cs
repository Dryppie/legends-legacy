using Application.Interfaces.Services.LL.Nobility;
using Domain.Models.MarketPlaces;
using Domain.Models.Nobility;

namespace Services.LL.Nobility;

// Called only inside the market transaction after its participant rows have been locked.
public sealed class SignetTradingService(INobilityRepository repository, TimeProvider time,
    Application.Interfaces.Services.LL.IStateSyncService? stateSync = null) : ISignetTradingService
{
    public async Task ReserveAsync(MarketPlaceListing listing, CancellationToken ct)
    {
        if (listing.ItemInstance.ItemBaseId != NobilityBenefits.SignetItemId) return;
        var units = (await repository.GetUnitsAsync(listing.SellerId, SignetState.Available, ct)).Take(listing.Quantity).ToArray();
        RequireQuantity(units, listing.Quantity);
        foreach (var unit in units)
        {
            unit.State = SignetState.Listed;
            unit.ListingId = listing.Id;
            Record(unit, listing.Id, SignetMovementKind.Reserved, listing.SellerId, listing.SellerId);
        }
        await repository.SynchronizeInventoryAsync(listing.SellerId, ct);
        await NotifyAsync(listing.SellerId, ct);
    }

    public async Task ReleaseAsync(MarketPlaceListing listing, CancellationToken ct)
    {
        if (listing.ItemInstance.ItemBaseId != NobilityBenefits.SignetItemId) return;
        var units = (await repository.GetUnitsAsync(listing.SellerId, SignetState.Listed, ct))
            .Where(x => x.ListingId == listing.Id).ToArray();
        RequireQuantity(units, listing.Quantity);
        foreach (var unit in units)
        {
            unit.State = SignetState.Available;
            unit.ListingId = null;
            Record(unit, listing.Id, SignetMovementKind.Released, listing.SellerId, listing.SellerId);
        }
        await repository.SynchronizeInventoryAsync(listing.SellerId, ct);
        await NotifyAsync(listing.SellerId, ct);
    }

    public async Task TradeAsync(MarketPlaceOrder trade, Guid? listingId, CancellationToken ct)
    {
        if (trade.ItemBaseId != NobilityBenefits.SignetItemId) return;
        var units = (await repository.GetUnitsAsync(trade.SellerId,
                listingId.HasValue ? SignetState.Listed : SignetState.Available, ct))
            .Where(x => x.ListingId == listingId).Take(trade.Quantity).ToArray();
        RequireQuantity(units, trade.Quantity);
        foreach (var unit in units)
        {
            unit.State = SignetState.Available;
            unit.ListingId = null;
            unit.OwnerCharacterId = trade.BuyerId;
            Record(unit, trade.Id, SignetMovementKind.Traded, trade.SellerId, trade.BuyerId);
        }
        await repository.SynchronizeInventoryAsync(trade.SellerId, ct);
        await repository.SynchronizeInventoryAsync(trade.BuyerId, ct);
        await NotifyAsync(trade.SellerId, ct);
        await NotifyAsync(trade.BuyerId, ct);
    }

    private Task NotifyAsync(Guid characterId, CancellationToken ct) => stateSync is null ? Task.CompletedTask :
        stateSync.InvalidateCharacterScopesAsync(characterId, [Application.WebSockets.Contracts.StateSyncScopes.Nobility], "SignetsChanged", ct);

    private static void RequireQuantity(SignetUnit[] units, int quantity)
    {
        if (quantity <= 0 || units.Length != quantity)
            throw new InvalidOperationException("Signet provenance does not match the market quantity. The transaction was cancelled.");
    }

    private void Record(SignetUnit unit, Guid operation, SignetMovementKind kind, Guid from, Guid to)
    {
        unit.Version = Guid.NewGuid();
        repository.AddMovement(new SignetMovement { UnitId = unit.Id, OperationId = operation,
            Kind = kind, FromCharacterId = from, ToCharacterId = to, OccurredAt = time.GetUtcNow() });
    }
}
