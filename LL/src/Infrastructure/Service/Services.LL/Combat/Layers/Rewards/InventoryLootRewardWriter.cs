using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Inventories;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class InventoryLootRewardWriter : ILootRewardWriter
{
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventPublisher _gameEventPublisher;
    private readonly IMapper _mapper;

    public InventoryLootRewardWriter(
        IInventoryService inventoryService,
        IGameEventPublisher gameEventPublisher,
        IMapper mapper)
    {
        _inventoryService = inventoryService;
        _gameEventPublisher = gameEventPublisher;
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

        var message = new LootReceivedMsg(
            ownerCharacterId,
            items.Select(x => _mapper.Map<InventoryItemDto>(x)).ToList());

        await _gameEventPublisher.PublishAsync(
            new Audience.Character(ownerCharacterId),
            message);
    }
}