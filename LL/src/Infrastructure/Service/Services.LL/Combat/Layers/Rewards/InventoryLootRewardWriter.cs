using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Dtos;
using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;
using AutoMapper;
using Domain.Models.Inventories;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class InventoryLootRewardWriter : ILootRewardWriter
{
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventPublisher _gameEventPublisher;
    private readonly IGameRealtimeBroadcasterV2 _gameRealtimeV2;
    private readonly IMapper _mapper;

    public InventoryLootRewardWriter(
        IInventoryService inventoryService,
        IGameEventPublisher gameEventPublisher,
        IGameRealtimeBroadcasterV2 gameRealtimeV2,
        IMapper mapper)
    {
        _inventoryService = inventoryService;
        _gameEventPublisher = gameEventPublisher;
        _gameRealtimeV2 = gameRealtimeV2;
        _mapper = mapper;
    }

    public async Task AddLootAsync(
        Guid ownerCharacterId,
        IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        await _inventoryService.AddItemsToInventory(
            ownerCharacterId,
            items.ToList(),
            cancellationToken);

        var mappedItems = items.Select(x => _mapper.Map<InventoryItemDto>(x)).ToList();
        var message = new LootReceivedMsg(ownerCharacterId, mappedItems);

        await _gameEventPublisher.PublishAsync(
            new Audience.Character(ownerCharacterId),
            message);

        await _gameRealtimeV2.PublishAsync(
            new Audience.Character(ownerCharacterId),
            new LootReceivedV2(ownerCharacterId, mappedItems, "combat-reward"),
            nameof(InventoryLootRewardWriter),
            cancellationToken);
    }
}
