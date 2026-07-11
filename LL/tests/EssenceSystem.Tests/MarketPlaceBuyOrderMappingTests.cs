using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class MarketPlaceBuyOrderMappingTests
{
    [Fact]
    public void BuyOrderProfiles_MapServiceResults()
    {
        var mapper = CreateMapper();
        var buyOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            BuyerName = "Buyer",
            ItemBaseId = "iron_ore",
            ItemBase = new ItemBase { Id = "iron_ore", Name = "Iron Ore" },
            Quantity = 4,
            UnitPrice = 12,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var itemInstanceId = Guid.NewGuid();
        var purchasedItem = new InventoryItem
        {
            InventoryId = buyOrder.BuyerId,
            ItemInstanceId = itemInstanceId,
            ItemInstance = new ItemInstance
            {
                Id = itemInstanceId,
                ItemBaseId = buyOrder.ItemBaseId,
                ItemBase = buyOrder.ItemBase
            },
            Quantity = 2
        };

        var createDto = mapper.Map<CreateMarketPlaceBuyOrderResponseDto>(
            new CreateMarketPlaceBuyOrderResult(buyOrder, 500));
        var fulfillDto = mapper.Map<FulfillMarketPlaceBuyOrderResponseDto>(
            new FulfillMarketPlaceBuyOrderResult(
                buyOrder.Id,
                buyOrder.BuyerId,
                Guid.NewGuid(),
                purchasedItem,
                null,
                buyOrder,
                2,
                24,
                524));
        var cancelDto = mapper.Map<CancelMarketPlaceBuyOrderResponseDto>(
            new CancelMarketPlaceBuyOrderResult(buyOrder.Id, 548));

        Assert.Equal(buyOrder.Id, createDto.BuyOrder.Id);
        Assert.Equal(500, createDto.BuyerCinders);
        Assert.Equal(itemInstanceId, fulfillDto.SoldItemInstanceId);
        Assert.Equal(2, fulfillDto.SoldQuantity);
        Assert.Equal(itemInstanceId, fulfillDto.PurchasedItem.ItemInstanceId);
        Assert.Equal(buyOrder.Id, fulfillDto.RemainingBuyOrder?.Id);
        Assert.Equal(548, cancelDto.BuyerCinders);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
