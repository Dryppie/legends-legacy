using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public class LootGeneratedEventHandler : INotificationHandler<LootGeneratedEvent>
{
    private readonly IInventoryService _inventoryService;
    public LootGeneratedEventHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Handle(LootGeneratedEvent notification, CancellationToken cancellationToken)
    {
        await _inventoryService.AddItemsToInventory(notification.CharacterId, notification.Loot, cancellationToken);
    }
}