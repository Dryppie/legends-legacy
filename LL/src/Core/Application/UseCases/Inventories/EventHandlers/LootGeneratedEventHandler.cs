using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Events;
using Application.WebSockets.Contracts;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public sealed class LootGeneratedEventHandler : INotificationHandler<LootGeneratedEvent>
{
    private readonly IInventoryService _inventory;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    public LootGeneratedEventHandler(IInventoryService inventory, IGameEventPublisher eventPublisher, IMapper mapper)
    {
        _inventory = inventory;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task Handle(LootGeneratedEvent notification, CancellationToken ct)
    {
        await _inventory.AddItemsToInventory(notification.CharacterId, notification.Loot, ct);

        var msg = new LootReceivedMsg(notification.CharacterId, notification.Loot.Select(i => _mapper.Map<InventoryItemDto>(i)).ToList());

        await _eventPublisher.PublishAsync(new Audience.Character(notification.CharacterId), msg);
    }
}