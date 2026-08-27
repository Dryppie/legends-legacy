using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Achievements;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Microsoft.Extensions.Options;

namespace Services.LL.MarketPlaces;

public class MarketPlaceService : IMarketPlaceService
{
    private readonly IMarketPlaceRepository _marketPlaceRepository;
    private readonly IItemBaseRepository _itemBaseRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterService _characterService;
    private readonly MarketPlaceOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IAchievementService? _achievementService;

    public MarketPlaceService(
        IMarketPlaceRepository marketPlaceRepository,
        IItemBaseRepository itemBaseRepository,
        IInventoryService inventoryService,
        ICharacterService characterService,
        IOptions<MarketPlaceOptions> options,
        TimeProvider timeProvider,
        IAchievementService? achievementService = null)
    {
        _marketPlaceRepository = marketPlaceRepository;
        _itemBaseRepository = itemBaseRepository;
        _inventoryService = inventoryService;
        _characterService = characterService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _achievementService = achievementService;
    }

    public async Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceListingsAsync(cancellationToken);
    }

    public async Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetMarketPlaceBuyOrdersAsync(cancellationToken);
    }

    public async Task<List<MarketPlaceOrder>> GetOrderHistoryAsync(Guid characterId, int take, CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetOrderHistoryAsync(characterId, take, cancellationToken);
    }

    public async Task<MarketPlaceItemSummary> GetItemSummaryAsync(string itemBaseId, CancellationToken cancellationToken)
    {
        return await _marketPlaceRepository.GetItemSummaryAsync(
            itemBaseId,
            _timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public async Task<CreateMarketPlaceListingResult?> CreateMarketPlaceListingAsync(
        Guid characterId,
        MarketPlaceListing marketPlaceListing,
        CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        if (!IsValidQuantityAndPrice(marketPlaceListing.Quantity, marketPlaceListing.UnitPrice)) return null;

        var inventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, marketPlaceListing.ItemInstanceId, cancellationToken);
        if (inventoryItem?.ItemInstance?.ItemBase == null || inventoryItem.ItemInstance.ItemBase.IsBound)
            return null;
        if (!inventoryItem.ItemInstance.ItemBase.Stackable && marketPlaceListing.Quantity != 1)
            return null;
        if (inventoryItem.Quantity < marketPlaceListing.Quantity)
            return null;

        var itemBase = inventoryItem.ItemInstance.ItemBase;
        var itemBaseId = inventoryItem.ItemInstance.ItemBaseId;
        var requestedQuantity = marketPlaceListing.Quantity;
        var now = _timeProvider.GetUtcNow();
        var remainingQuantity = requestedQuantity;
        var plan = new List<(MarketPlaceBuyOrder Order, int Quantity)>();

        if (itemBase.Stackable)
        {
            var candidates = await _marketPlaceRepository.GetCommodityBuyOrdersAsync(
                itemBaseId,
                marketPlaceListing.UnitPrice,
                cancellationToken);
            foreach (var candidate in candidates
                         .Where(x => x.BuyerId != characterId)
                         .OrderByDescending(x => x.UnitPrice)
                         .ThenBy(x => x.CreatedAt))
            {
                var locked = await _marketPlaceRepository.GetBuyOrderAsync(candidate.Id, cancellationToken);
                if (locked == null ||
                    locked.BuyerId == characterId ||
                    locked.ExpiresAt <= now ||
                    locked.UnitPrice < marketPlaceListing.UnitPrice ||
                    !string.Equals(locked.ItemBaseId, itemBaseId, StringComparison.Ordinal) ||
                    locked.ItemBase.IsBound ||
                    !locked.ItemBase.Stackable)
                {
                    continue;
                }

                var fillQuantity = Math.Min(remainingQuantity, locked.Quantity);
                if (fillQuantity <= 0) continue;

                plan.Add((locked, fillQuantity));
                remainingQuantity -= fillQuantity;
                if (remainingQuantity == 0) break;
            }
        }

        var participantIds = plan
            .Select(x => x.Order.BuyerId)
            .Append(characterId)
            .Distinct()
            .ToArray();
        await _marketPlaceRepository.LockCharactersAsync(participantIds, cancellationToken);

        if (await _marketPlaceRepository.HasActiveBuyOrderForItemAsync(
                characterId,
                itemBaseId,
                now,
                cancellationToken))
        {
            return null;
        }

        if (remainingQuantity > 0 &&
            itemBase.Stackable &&
            await _marketPlaceRepository.HasActiveListingForItemAsync(
                characterId,
                itemBaseId,
                now,
                cancellationToken))
        {
            return null;
        }

        if (remainingQuantity > 0 &&
            await _marketPlaceRepository.GetListingCountAsync(characterId, cancellationToken) >= _options.MaximumListingsPerCharacter)
        {
            return null;
        }

        var seller = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (seller == null) return null;

        var remainingInventoryItem = CreateRemainingInventoryItem(inventoryItem, requestedQuantity);

        var removalListing = new MarketPlaceListing
        {
            SellerId = characterId,
            ItemInstanceId = inventoryItem.ItemInstanceId,
            ItemInstance = inventoryItem.ItemInstance,
            Quantity = requestedQuantity,
            UnitPrice = marketPlaceListing.UnitPrice,
            CreatedAt = now
        };
        if (!await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(
                characterId,
                removalListing,
                cancellationToken))
        {
            return null;
        }

        MarketPlaceListing? createdListing = null;
        if (remainingQuantity > 0)
        {
            marketPlaceListing.ItemInstance = inventoryItem.ItemInstance;
            marketPlaceListing.Quantity = remainingQuantity;
            marketPlaceListing.CreatedAt = now;
            marketPlaceListing.ExpiresAt = now.AddDays(_options.OrderLifetimeDays);
            createdListing = await _marketPlaceRepository.CreateMarketPlaceListingAsync(
                characterId,
                marketPlaceListing,
                cancellationToken);
            if (createdListing == null)
            {
                await _inventoryService.AddItemToInventoryFromMarketPlace(characterId, new InventoryItem
                {
                    InventoryId = characterId,
                    ItemInstanceId = inventoryItem.ItemInstanceId,
                    ItemInstance = inventoryItem.ItemInstance,
                    Quantity = requestedQuantity,
                    SeenAtUtc = now
                }, cancellationToken);
                return null;
            }

        }

        long totalPrice = 0;
        long totalFees = 0;
        var fills = new List<FulfillMarketPlaceBuyOrderResult>(plan.Count);
        foreach (var (order, fillQuantity) in plan)
        {
            var fillTotal = checked(order.UnitPrice * fillQuantity);
            var fee = CalculateSellerFee(fillTotal);
            totalPrice = checked(totalPrice + fillTotal);
            totalFees = checked(totalFees + fee);
            seller.Cinders = checked(seller.Cinders + fillTotal - fee);

            var purchasedItem = new InventoryItem
            {
                InventoryId = order.BuyerId,
                Quantity = fillQuantity,
                ItemInstanceId = inventoryItem.ItemInstanceId,
                ItemInstance = inventoryItem.ItemInstance
            };
            await AddMarketplacePurchaseAsync(
                order.BuyerId,
                purchasedItem,
                now,
                cancellationToken);

            order.Quantity -= fillQuantity;
            if (order.Quantity == 0)
            {
                _marketPlaceRepository.RemoveBuyOrder(order);
            }

            var trade = new MarketPlaceOrder
            {
                Id = Guid.NewGuid(),
                SellerId = characterId,
                BuyerId = order.BuyerId,
                ItemBaseId = itemBaseId,
                ItemBase = order.ItemBase,
                ItemInstanceId = null,
                Quantity = fillQuantity,
                UnitPrice = order.UnitPrice,
                TotalPrice = fillTotal,
                SellerFee = fee,
                Source = MarketPlaceTradeSource.BuyOrder,
                PurchasedAt = now
            };
            await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
            await RecordMarketplaceSaleAsync(characterId, cancellationToken);

            fills.Add(new FulfillMarketPlaceBuyOrderResult(
                order.Id,
                order.BuyerId,
                characterId,
                purchasedItem,
                null,
                order.Quantity > 0 ? order : null,
                fillQuantity,
                fillTotal,
                fee,
                seller.Cinders,
                trade));
        }

        fills = fills.Select(fill => fill with
        {
            RemainingSellerInventoryItem = remainingInventoryItem
        }).ToList();

        return new CreateMarketPlaceListingResult(
            createdListing,
            fills,
            remainingInventoryItem,
            requestedQuantity - remainingQuantity,
            totalPrice,
            totalFees,
            seller.Cinders);
    }

    public async Task<CreateMarketPlaceBuyOrderResult?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder buyOrder, CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        if (!IsValidQuantityAndPrice(buyOrder.Quantity, buyOrder.UnitPrice))
            return null;

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync([buyOrder.ItemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(buyOrder.ItemBaseId, out var itemBase) || !itemBase.Stackable || itemBase.IsBound) return null;

        var requestedQuantity = buyOrder.Quantity;
        var now = _timeProvider.GetUtcNow();
        var candidates = await _marketPlaceRepository.GetCommodityListingsAsync(
            buyOrder.ItemBaseId,
            buyOrder.UnitPrice,
            cancellationToken);
        var remainingQuantity = requestedQuantity;
        var plan = new List<(MarketPlaceListing Listing, int Quantity)>();

        foreach (var candidate in candidates
                     .Where(x => x.SellerId != characterId)
                     .OrderBy(x => x.UnitPrice)
                     .ThenBy(x => x.CreatedAt))
        {
            var locked = await _marketPlaceRepository.GetListingAsync(candidate.Id, cancellationToken);
            if (locked == null ||
                locked.SellerId == characterId ||
                locked.ExpiresAt <= now ||
                locked.UnitPrice > buyOrder.UnitPrice ||
                !string.Equals(locked.ItemInstance.ItemBaseId, buyOrder.ItemBaseId, StringComparison.Ordinal) ||
                locked.ItemInstance.ItemBase.IsBound ||
                !locked.ItemInstance.ItemBase.Stackable)
            {
                continue;
            }

            var fillQuantity = Math.Min(remainingQuantity, locked.Quantity);
            if (fillQuantity <= 0) continue;

            plan.Add((locked, fillQuantity));
            remainingQuantity -= fillQuantity;
            if (remainingQuantity == 0) break;
        }

        var participantIds = plan
            .Select(x => x.Listing.SellerId)
            .Append(characterId)
            .Distinct()
            .ToArray();
        await _marketPlaceRepository.LockCharactersAsync(participantIds, cancellationToken);

        if (await _marketPlaceRepository.HasActiveListingForItemAsync(
                characterId,
                buyOrder.ItemBaseId,
                now,
                cancellationToken))
        {
            return null;
        }

        if (remainingQuantity > 0 &&
            await _marketPlaceRepository.HasActiveBuyOrderForItemAsync(
                characterId,
                buyOrder.ItemBaseId,
                now,
                cancellationToken))
        {
            return null;
        }

        if (remainingQuantity > 0 &&
            await _marketPlaceRepository.GetBuyOrderCountAsync(characterId, cancellationToken) >= _options.MaximumBuyOrdersPerCharacter)
        {
            return null;
        }

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;

        var sellers = new Dictionary<Guid, Character>();
        foreach (var sellerId in plan.Select(x => x.Listing.SellerId).Distinct())
        {
            var seller = await _characterService.GetCharacterByCharacterIdAsync(sellerId, cancellationToken);
            if (seller == null)
                throw new InvalidOperationException("A marketplace seller disappeared during atomic buy-order matching.");

            sellers.Add(sellerId, seller);
        }

        long filledTotalPrice;
        long escrowTotal;
        long requiredCinders;
        try
        {
            filledTotalPrice = plan.Aggregate(
                0L,
                (total, fill) => checked(total + checked(fill.Listing.UnitPrice * fill.Quantity)));
            escrowTotal = checked(buyOrder.UnitPrice * remainingQuantity);
            requiredCinders = checked(filledTotalPrice + escrowTotal);
        }
        catch (OverflowException)
        {
            return null;
        }

        if (buyer.Cinders < requiredCinders) return null;

        MarketPlaceBuyOrder? createdBuyOrder = null;
        if (remainingQuantity > 0)
        {
            buyOrder.Quantity = remainingQuantity;
            buyOrder.ItemBase = itemBase;
            buyOrder.CreatedAt = now;
            buyOrder.ExpiresAt = now.AddDays(_options.OrderLifetimeDays);

            createdBuyOrder = await _marketPlaceRepository.CreateMarketPlaceBuyOrderAsync(
                characterId,
                buyOrder,
                cancellationToken);
            if (createdBuyOrder == null) return null;

        }

        buyer.Cinders -= requiredCinders;
        var fills = new List<BuyoutMarketPlaceListingResult>(plan.Count);
        foreach (var (listing, fillQuantity) in plan)
        {
            var seller = sellers[listing.SellerId];
            var fillTotal = checked(listing.UnitPrice * fillQuantity);
            var fee = CalculateSellerFee(fillTotal);
            seller.Cinders += fillTotal - fee;

            var purchasedItem = new InventoryItem
            {
                InventoryId = characterId,
                Quantity = fillQuantity,
                ItemInstanceId = listing.ItemInstanceId,
                ItemInstance = listing.ItemInstance,
            };
            await AddMarketplacePurchaseAsync(
                characterId,
                purchasedItem,
                now,
                cancellationToken);

            listing.Quantity -= fillQuantity;
            if (listing.Quantity == 0)
            {
                _marketPlaceRepository.RemoveListingAsync(listing);
            }

            var trade = new MarketPlaceOrder
            {
                Id = Guid.NewGuid(),
                SellerId = listing.SellerId,
                BuyerId = characterId,
                ItemBaseId = buyOrder.ItemBaseId,
                ItemBase = listing.ItemInstance.ItemBase,
                ItemInstanceId = null,
                Quantity = fillQuantity,
                UnitPrice = listing.UnitPrice,
                TotalPrice = fillTotal,
                SellerFee = fee,
                Source = MarketPlaceTradeSource.SellListing,
                PurchasedAt = now
            };
            await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
            await RecordMarketplaceSaleAsync(listing.SellerId, cancellationToken);

            fills.Add(new BuyoutMarketPlaceListingResult(
                listing.Id,
                listing.SellerId,
                purchasedItem,
                listing.Quantity > 0 ? listing : null,
                fillQuantity,
                fillTotal,
                buyer.Cinders,
                seller.Cinders,
                trade));
        }

        return new CreateMarketPlaceBuyOrderResult(
            createdBuyOrder,
            fills,
            requestedQuantity - remainingQuantity,
            filledTotalPrice,
            buyer.Cinders);
    }

    public async Task<BuyoutMarketPlaceListingResult?> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        var now = _timeProvider.GetUtcNow();
        var listing = await _marketPlaceRepository.GetListingAsync(listingId, cancellationToken);
        if (listing is not null &&
            !await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                listing.SellerId,
                cancellationToken)) return null;
        if (listing == null || listing.ExpiresAt <= now || listing.Quantity < quantity || listing.SellerId.Equals(characterId) ||
            !IsValidQuantityAndPrice(quantity, listing.UnitPrice) ||
            listing.ItemInstance.ItemBase.IsBound ||
            (!listing.ItemInstance.ItemBase.Stackable && quantity != 1) ||
            !TryCalculateTotal(listing.UnitPrice, quantity, out var totalPrice))
            return null;

        await _marketPlaceRepository.LockCharactersAsync([characterId, listing.SellerId], cancellationToken);
        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;
        var seller = await _characterService.GetCharacterByCharacterIdAsync(listing.SellerId, cancellationToken);
        if (seller == null) return null;

        if (buyer.Cinders < totalPrice) return null;

        var fee = CalculateSellerFee(totalPrice);
        buyer.Cinders -= totalPrice;
        seller.Cinders += totalPrice - fee;

        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            Quantity = quantity,
            ItemInstanceId = listing.ItemInstanceId,
            ItemInstance = listing.ItemInstance,
        };

        await AddMarketplacePurchaseAsync(characterId, inventoryItem, now, cancellationToken);

        listing.Quantity -= quantity;
        if (listing.Quantity == 0)
        {
            _marketPlaceRepository.RemoveListingAsync(listing);
        }

        var trade = new MarketPlaceOrder
        {
            Id = Guid.NewGuid(),
            SellerId = listing.SellerId,
            BuyerId = characterId,
            ItemBaseId = listing.ItemInstance.ItemBaseId,
            ItemBase = listing.ItemInstance.ItemBase,
            ItemInstanceId = listing.ItemInstance.ItemBase.Stackable ? null : listing.ItemInstanceId,
            Quantity = quantity,
            UnitPrice = listing.UnitPrice,
            TotalPrice = totalPrice,
            SellerFee = fee,
            Source = MarketPlaceTradeSource.SellListing,
            PurchasedAt = now
        };
        await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
        await RecordMarketplaceSaleAsync(listing.SellerId, cancellationToken);

        return new BuyoutMarketPlaceListingResult(
            listingId,
            listing.SellerId,
            inventoryItem,
            listing.Quantity > 0 ? listing : null,
            quantity,
            totalPrice,
            buyer.Cinders,
            seller.Cinders,
            trade);
    }

    public async Task<BuyCommodityResult?> BuyCommodityAsync(
        Guid characterId,
        string itemBaseId,
        int quantity,
        long maximumUnitPrice,
        CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        if (string.IsNullOrWhiteSpace(itemBaseId) || !IsValidQuantityAndPrice(quantity, maximumUnitPrice))
            return null;

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(itemBaseId, out var itemBase) || !itemBase.Stackable || itemBase.IsBound)
            return null;

        var now = _timeProvider.GetUtcNow();
        var candidates = await _marketPlaceRepository.GetCommodityListingsAsync(
            itemBaseId,
            maximumUnitPrice,
            cancellationToken);

        var remaining = quantity;
        var plan = new List<(MarketPlaceListing Listing, int Quantity)>();
        foreach (var candidate in candidates.Where(x => x.SellerId != characterId))
        {
            var locked = await _marketPlaceRepository.GetListingAsync(candidate.Id, cancellationToken);
            if (locked == null ||
                locked.SellerId == characterId ||
                locked.ExpiresAt <= now ||
                locked.UnitPrice > maximumUnitPrice ||
                !string.Equals(locked.ItemInstance.ItemBaseId, itemBaseId, StringComparison.Ordinal) ||
                locked.ItemInstance.ItemBase.IsBound ||
                !locked.ItemInstance.ItemBase.Stackable)
            {
                continue;
            }

            var fillQuantity = Math.Min(remaining, locked.Quantity);
            if (fillQuantity <= 0) continue;
            plan.Add((locked, fillQuantity));
            remaining -= fillQuantity;
            if (remaining == 0) break;
        }

        if (remaining != 0 || plan.Count == 0) return null;

        var participantIds = plan
            .Select(x => x.Listing.SellerId)
            .Append(characterId)
            .Distinct()
            .ToArray();
        await _marketPlaceRepository.LockCharactersAsync(participantIds, cancellationToken);

        long totalPrice;
        try
        {
            totalPrice = plan.Aggregate(
                0L,
                (total, fill) => checked(total + checked(fill.Listing.UnitPrice * fill.Quantity)));
        }
        catch (OverflowException)
        {
            return null;
        }

        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null || buyer.Cinders < totalPrice) return null;

        var fills = new List<BuyoutMarketPlaceListingResult>(plan.Count);
        foreach (var (listing, fillQuantity) in plan)
        {
            var seller = await _characterService.GetCharacterByCharacterIdAsync(listing.SellerId, cancellationToken);
            if (seller == null)
                throw new InvalidOperationException("A marketplace seller disappeared during an atomic purchase.");

            var fillTotal = checked(listing.UnitPrice * fillQuantity);
            var fee = CalculateSellerFee(fillTotal);
            buyer.Cinders -= fillTotal;
            seller.Cinders += fillTotal - fee;

            var purchasedItem = new InventoryItem
            {
                InventoryId = characterId,
                Quantity = fillQuantity,
                ItemInstanceId = listing.ItemInstanceId,
                ItemInstance = listing.ItemInstance,
            };
            await AddMarketplacePurchaseAsync(
                characterId,
                purchasedItem,
                now,
                cancellationToken);

            listing.Quantity -= fillQuantity;
            if (listing.Quantity == 0)
            {
                _marketPlaceRepository.RemoveListingAsync(listing);
            }

            var trade = new MarketPlaceOrder
            {
                Id = Guid.NewGuid(),
                SellerId = listing.SellerId,
                BuyerId = characterId,
                ItemBaseId = itemBaseId,
                ItemBase = listing.ItemInstance.ItemBase,
                ItemInstanceId = null,
                Quantity = fillQuantity,
                UnitPrice = listing.UnitPrice,
                TotalPrice = fillTotal,
                SellerFee = fee,
                Source = MarketPlaceTradeSource.SellListing,
                PurchasedAt = now
            };
            await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
            await RecordMarketplaceSaleAsync(listing.SellerId, cancellationToken);

            fills.Add(new BuyoutMarketPlaceListingResult(
                listing.Id,
                listing.SellerId,
                purchasedItem,
                listing.Quantity > 0 ? listing : null,
                fillQuantity,
                fillTotal,
                buyer.Cinders,
                seller.Cinders,
                trade));
        }

        return new BuyCommodityResult(fills, quantity, totalPrice, buyer.Cinders);
    }

    public async Task<FulfillMarketPlaceBuyOrderResult?> FulfillMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        var now = _timeProvider.GetUtcNow();
        var buyOrder = await _marketPlaceRepository.GetBuyOrderAsync(buyOrderId, cancellationToken);
        if (buyOrder is not null &&
            !await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                buyOrder.BuyerId,
                cancellationToken)) return null;
        if (buyOrder == null || buyOrder.ExpiresAt <= now || buyOrder.Quantity < quantity || buyOrder.BuyerId.Equals(characterId) ||
            !IsValidQuantityAndPrice(quantity, buyOrder.UnitPrice) ||
            !TryCalculateTotal(buyOrder.UnitPrice, quantity, out var totalPrice)) return null;

        var sellerInventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);
        if (sellerInventoryItem == null || sellerInventoryItem.Quantity < quantity) return null;
        if (!sellerInventoryItem.ItemInstance.ItemBase.Stackable || sellerInventoryItem.ItemInstance.ItemBase.IsBound) return null;
        if (!string.Equals(sellerInventoryItem.ItemInstance.ItemBase.Id, buyOrder.ItemBaseId, StringComparison.Ordinal)) return null;

        await _marketPlaceRepository.LockCharactersAsync([characterId, buyOrder.BuyerId], cancellationToken);
        var buyer = await _characterService.GetCharacterByCharacterIdAsync(buyOrder.BuyerId, cancellationToken);
        if (buyer == null) return null;
        var seller = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (seller == null) return null;

        var removalListing = new MarketPlaceListing
        {
            SellerId = characterId,
            ItemInstanceId = itemInstanceId,
            Quantity = quantity,
            UnitPrice = buyOrder.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var removed = await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(characterId, removalListing, cancellationToken);
        if (!removed) return null;

        var fee = CalculateSellerFee(totalPrice);
        seller.Cinders += totalPrice - fee;

        var purchasedItem = new InventoryItem
        {
            InventoryId = buyOrder.BuyerId,
            Quantity = quantity,
            ItemInstanceId = sellerInventoryItem.ItemInstanceId,
            ItemInstance = sellerInventoryItem.ItemInstance,
        };

        await AddMarketplacePurchaseAsync(buyOrder.BuyerId, purchasedItem, now, cancellationToken);

        buyOrder.Quantity -= quantity;
        if (buyOrder.Quantity == 0)
        {
            _marketPlaceRepository.RemoveBuyOrder(buyOrder);
        }

        var trade = new MarketPlaceOrder
        {
            Id = Guid.NewGuid(),
            SellerId = characterId,
            BuyerId = buyOrder.BuyerId,
            ItemBaseId = buyOrder.ItemBaseId,
            ItemBase = buyOrder.ItemBase,
            ItemInstanceId = null,
            Quantity = quantity,
            UnitPrice = buyOrder.UnitPrice,
            TotalPrice = totalPrice,
            SellerFee = fee,
            Source = MarketPlaceTradeSource.BuyOrder,
            PurchasedAt = now
        };
        await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
        await RecordMarketplaceSaleAsync(characterId, cancellationToken);

        var remainingSellerInventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);

        return new FulfillMarketPlaceBuyOrderResult(
            buyOrderId,
            buyOrder.BuyerId,
            characterId,
            purchasedItem,
            remainingSellerInventoryItem,
            buyOrder.Quantity > 0 ? buyOrder : null,
            quantity,
            totalPrice,
            fee,
            seller.Cinders,
            trade);
    }

    public async Task<SellCommodityResult?> SellCommodityAsync(
        Guid characterId,
        Guid itemInstanceId,
        int quantity,
        long minimumUnitPrice,
        CancellationToken cancellationToken)
    {
        if (!await _marketPlaceRepository.IsCharacterMultiplayerEligibleAsync(
                characterId,
                cancellationToken)) return null;
        if (!IsValidQuantityAndPrice(quantity, minimumUnitPrice)) return null;

        var inventoryItem = await _inventoryService.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);
        if (inventoryItem?.ItemInstance?.ItemBase == null ||
            inventoryItem.Quantity < quantity ||
            !inventoryItem.ItemInstance.ItemBase.Stackable ||
            inventoryItem.ItemInstance.ItemBase.IsBound)
            return null;

        var itemBaseId = inventoryItem.ItemInstance.ItemBaseId;
        var now = _timeProvider.GetUtcNow();
        var candidates = await _marketPlaceRepository.GetCommodityBuyOrdersAsync(
            itemBaseId,
            minimumUnitPrice,
            cancellationToken);

        var remaining = quantity;
        var plan = new List<(MarketPlaceBuyOrder Order, int Quantity)>();
        foreach (var candidate in candidates.Where(x => x.BuyerId != characterId))
        {
            var locked = await _marketPlaceRepository.GetBuyOrderAsync(candidate.Id, cancellationToken);
            if (locked == null ||
                locked.BuyerId == characterId ||
                locked.ExpiresAt <= now ||
                locked.UnitPrice < minimumUnitPrice ||
                !string.Equals(locked.ItemBaseId, itemBaseId, StringComparison.Ordinal) ||
                locked.ItemBase.IsBound ||
                !locked.ItemBase.Stackable)
            {
                continue;
            }

            var fillQuantity = Math.Min(remaining, locked.Quantity);
            if (fillQuantity <= 0) continue;
            plan.Add((locked, fillQuantity));
            remaining -= fillQuantity;
            if (remaining == 0) break;
        }

        if (remaining != 0 || plan.Count == 0) return null;

        var remainingInventoryItem = CreateRemainingInventoryItem(inventoryItem, quantity);

        var participantIds = plan
            .Select(x => x.Order.BuyerId)
            .Append(characterId)
            .Distinct()
            .ToArray();
        await _marketPlaceRepository.LockCharactersAsync(participantIds, cancellationToken);

        var removalListing = new MarketPlaceListing
        {
            SellerId = characterId,
            ItemInstanceId = itemInstanceId,
            ItemInstance = inventoryItem.ItemInstance,
            Quantity = quantity,
            UnitPrice = minimumUnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };
        if (!await _inventoryService.TryRemoveItemsForMarketPlaceListingAsync(
                characterId,
                removalListing,
                cancellationToken))
            return null;

        var seller = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (seller == null)
            throw new InvalidOperationException("A marketplace seller disappeared during an atomic sale.");

        long totalPrice = 0;
        long totalFees = 0;
        var fills = new List<FulfillMarketPlaceBuyOrderResult>(plan.Count);
        foreach (var (order, fillQuantity) in plan)
        {
            var buyer = await _characterService.GetCharacterByCharacterIdAsync(order.BuyerId, cancellationToken);
            if (buyer == null)
                throw new InvalidOperationException("A marketplace buyer disappeared during an atomic sale.");

            var fillTotal = checked(order.UnitPrice * fillQuantity);
            var fee = CalculateSellerFee(fillTotal);
            totalPrice = checked(totalPrice + fillTotal);
            totalFees = checked(totalFees + fee);
            seller.Cinders += fillTotal - fee;

            var purchasedItem = new InventoryItem
            {
                InventoryId = order.BuyerId,
                Quantity = fillQuantity,
                ItemInstanceId = inventoryItem.ItemInstanceId,
                ItemInstance = inventoryItem.ItemInstance,
            };
            await AddMarketplacePurchaseAsync(
                order.BuyerId,
                purchasedItem,
                now,
                cancellationToken);

            order.Quantity -= fillQuantity;
            if (order.Quantity == 0)
            {
                _marketPlaceRepository.RemoveBuyOrder(order);
            }

            var trade = new MarketPlaceOrder
            {
                Id = Guid.NewGuid(),
                SellerId = characterId,
                BuyerId = order.BuyerId,
                ItemBaseId = itemBaseId,
                ItemBase = order.ItemBase,
                ItemInstanceId = null,
                Quantity = fillQuantity,
                UnitPrice = order.UnitPrice,
                TotalPrice = fillTotal,
                SellerFee = fee,
                Source = MarketPlaceTradeSource.BuyOrder,
                PurchasedAt = now
            };
            await _marketPlaceRepository.AddOrderAsync(trade, cancellationToken);
            await RecordMarketplaceSaleAsync(characterId, cancellationToken);

            fills.Add(new FulfillMarketPlaceBuyOrderResult(
                order.Id,
                order.BuyerId,
                characterId,
                purchasedItem,
                null,
                order.Quantity > 0 ? order : null,
                fillQuantity,
                fillTotal,
                fee,
                seller.Cinders,
                trade));
        }

        fills = fills.Select(fill => fill with
        {
            RemainingSellerInventoryItem = remainingInventoryItem
        }).ToList();

        return new SellCommodityResult(fills, quantity, totalPrice, totalFees, seller.Cinders);
    }

    private static InventoryItem? CreateRemainingInventoryItem(InventoryItem inventoryItem, int removedQuantity)
    {
        var remainingQuantity = inventoryItem.Quantity - removedQuantity;
        if (remainingQuantity <= 0) return null;

        return new InventoryItem
        {
            InventoryId = inventoryItem.InventoryId,
            ItemInstanceId = inventoryItem.ItemInstanceId,
            ItemInstance = inventoryItem.ItemInstance,
            Quantity = remainingQuantity
        };
    }

    public async Task<InventoryItem?> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await _marketPlaceRepository.GetListingAsync(listingId, cancellationToken);
        if (listing == null) return null;
        if (listing.SellerId != characterId) return null;

        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            Quantity = listing.Quantity,
            ItemInstanceId = listing.ItemInstanceId,
            ItemInstance = listing.ItemInstance,
            SeenAtUtc = _timeProvider.GetUtcNow()
        };

        await _inventoryService.AddItemToInventoryFromMarketPlace(characterId, inventoryItem, cancellationToken);
        _marketPlaceRepository.RemoveListingAsync(listing);

        return inventoryItem;
    }

    public async Task<CancelMarketPlaceBuyOrderResult?> CancelMarketPlaceBuyOrderAsync(Guid characterId, Guid buyOrderId, CancellationToken cancellationToken)
    {
        var buyOrder = await _marketPlaceRepository.GetBuyOrderAsync(buyOrderId, cancellationToken);
        if (buyOrder == null) return null;
        if (buyOrder.BuyerId != characterId) return null;

        await _marketPlaceRepository.LockCharactersAsync([characterId], cancellationToken);
        var buyer = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (buyer == null) return null;

        var refund = checked(buyOrder.UnitPrice * buyOrder.Quantity);
        buyer.Cinders = checked(buyer.Cinders + refund);
        _marketPlaceRepository.RemoveBuyOrder(buyOrder);

        return new CancelMarketPlaceBuyOrderResult(buyOrderId, buyer.Cinders);
    }

    public async Task<ExpireMarketPlaceOrdersResult> ExpireOrdersAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(batchSize, 1, 5_000);
        var expiredListings = 0;
        var expiredBuyOrders = 0;
        long refundedCinders = 0;
        var affectedCharacterIds = new HashSet<Guid>();

        var listingIds = await _marketPlaceRepository.GetExpiredListingIdsAsync(now, take, cancellationToken);
        foreach (var listingId in listingIds)
        {
            var listing = await _marketPlaceRepository.GetListingAsync(listingId, cancellationToken);
            if (listing == null || listing.ExpiresAt > now)
                continue;

            await _inventoryService.AddItemToInventoryFromMarketPlace(listing.SellerId, new InventoryItem
            {
                InventoryId = listing.SellerId,
                Quantity = listing.Quantity,
                ItemInstanceId = listing.ItemInstanceId,
                ItemInstance = listing.ItemInstance,
                SeenAtUtc = now
            }, cancellationToken);

            _marketPlaceRepository.RemoveListingAsync(listing);
            affectedCharacterIds.Add(listing.SellerId);
            expiredListings++;
        }

        var buyOrderIds = await _marketPlaceRepository.GetExpiredBuyOrderIdsAsync(now, take, cancellationToken);
        foreach (var buyOrderId in buyOrderIds)
        {
            var buyOrder = await _marketPlaceRepository.GetBuyOrderAsync(buyOrderId, cancellationToken);
            if (buyOrder == null || buyOrder.ExpiresAt > now)
                continue;

            await _marketPlaceRepository.LockCharactersAsync([buyOrder.BuyerId], cancellationToken);
            var buyer = await _characterService.GetCharacterByCharacterIdAsync(buyOrder.BuyerId, cancellationToken);
            if (buyer == null)
                throw new InvalidOperationException($"Cannot refund expired marketplace buy order {buyOrder.Id}: buyer {buyOrder.BuyerId} was not found.");

            var refund = checked(buyOrder.UnitPrice * buyOrder.Quantity);
            buyer.Cinders = checked(buyer.Cinders + refund);
            refundedCinders = checked(refundedCinders + refund);
            _marketPlaceRepository.RemoveBuyOrder(buyOrder);
            affectedCharacterIds.Add(buyOrder.BuyerId);
            expiredBuyOrders++;
        }

        return new ExpireMarketPlaceOrdersResult(
            expiredListings,
            expiredBuyOrders,
            refundedCinders,
            affectedCharacterIds);
    }

    public async Task<List<ItemBase>> GetTradableItemBasesAsync(CancellationToken cancellationToken)
    {
        return await _itemBaseRepository.GetTradableItemBasesAsync(cancellationToken);
    }

    private bool IsValidQuantityAndPrice(int quantity, long unitPrice) =>
        quantity is > 0 && quantity <= _options.MaximumStackQuantity &&
        unitPrice is > 0 && unitPrice <= _options.MaximumUnitPrice;

    private static bool TryCalculateTotal(long unitPrice, int quantity, out long total)
    {
        try
        {
            total = checked(unitPrice * quantity);
            return true;
        }
        catch (OverflowException)
        {
            total = 0;
            return false;
        }
    }

    private long CalculateSellerFee(long totalPrice)
    {
        if (_options.SellerFeeBasisPoints <= 0 || totalPrice <= 0) return 0;

        var proportionalFee = (long)decimal.Ceiling(
            totalPrice * (decimal)_options.SellerFeeBasisPoints / 10_000m);
        return Math.Min(totalPrice, Math.Max(_options.MinimumSellerFee, proportionalFee));
    }

    private async Task AddMarketplacePurchaseAsync(
        Guid buyerId,
        InventoryItem purchasedItem,
        DateTimeOffset purchasedAt,
        CancellationToken cancellationToken)
    {
        purchasedItem.ItemInstance.AcquisitionSource = ItemAcquisitionSources.Marketplace;
        purchasedItem.ItemInstance.AcquiredAtUtc = purchasedAt;
        await _inventoryService.AddItemToInventoryFromMarketPlace(
            buyerId,
            purchasedItem,
            cancellationToken);
    }

    private async Task RecordMarketplaceSaleAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        if (_achievementService is not null)
        {
            await _achievementService.RecordMarketplaceSaleAsync(sellerId, cancellationToken);
        }
    }
}
