using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Events;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Items;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public sealed class LootGeneratedEventHandler : INotificationHandler<LootGeneratedEvent>
{
    private readonly IInventoryService _inventory;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;
    public LootGeneratedEventHandler(IInventoryService inventory, IGameRealtimeBroadcaster gameRealtime, IMapper mapper)
    {
        _inventory = inventory;
        _gameRealtime = gameRealtime;
        _mapper = mapper;
    }

    public async Task Handle(LootGeneratedEvent notification, CancellationToken ct)
    {
        await _inventory.AddItemsToInventory(
            notification.CharacterId,
            notification.Loot,
            ItemAcquisitionSources.LootGeneratedEvent,
            ct);

        var items = notification.Loot.Select(i => _mapper.Map<InventoryItemDto>(i)).ToList();
        await _gameRealtime.PublishAsync(
            new Audience.Character(notification.CharacterId),
            new LootReceived(notification.CharacterId, items, "combat-reward", null),
            nameof(LootGeneratedEventHandler),
            ct);
    }
}
