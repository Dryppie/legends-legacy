using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;

namespace Application.UseCases.MarketPlaces;

public sealed class MarketplaceChangePublisher(
    IStateSyncService stateSync,
    IGameRealtimeBroadcaster events)
{
    public async Task<MarketplaceChangeSetDto> PublishAsync(
        IReadOnlyList<MarketplaceListingChangeDto> listingChanges,
        IReadOnlyList<MarketplaceBuyOrderChangeDto> buyOrderChanges,
        IReadOnlyList<MarketPlaceOrderDto> orders,
        IEnumerable<Guid> affectedCharacterIds,
        string reason,
        CancellationToken cancellationToken)
    {
        var version = await stateSync.AdvanceWorldScopeWithRevisionAsync(
            StateSyncScopes.Marketplace,
            reason,
            cancellationToken);
        if (version < 1)
        {
            throw new InvalidOperationException("Marketplace version allocation failed.");
        }

        var changes = new MarketplaceChangeSetDto
        {
            Version = version,
            ListingChanges = listingChanges,
            BuyOrderChanges = buyOrderChanges,
            Orders = orders,
            AffectedCharacterIds = affectedCharacterIds
                .Where(characterId => characterId != Guid.Empty)
                .Distinct()
                .Order()
                .ToArray()
        };

        await events.PublishAsync(
            new Audience.World(),
            new MarketplaceChanged(changes),
            nameof(MarketplaceChangePublisher),
            cancellationToken);
        return changes;
    }
}
