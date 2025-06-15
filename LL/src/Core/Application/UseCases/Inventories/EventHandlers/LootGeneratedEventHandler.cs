using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public sealed class LootGeneratedEventHandler : INotificationHandler<LootGeneratedEvent>
{
    private readonly IInventoryService _inventory;
    public LootGeneratedEventHandler(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    public async Task Handle(LootGeneratedEvent notification, CancellationToken ct)
        => await _inventory.AddItemsToInventory(notification.CharacterId, notification.Loot, ct);
}