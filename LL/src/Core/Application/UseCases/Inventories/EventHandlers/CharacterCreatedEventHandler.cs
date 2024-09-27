using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Events;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public class CharacterCreatedEventHandler : INotificationHandler<CharacterCreatedEvent>
{
    private readonly IInventoryService _inventoryService;
    public CharacterCreatedEventHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("INVENTORY");
        await _inventoryService.CreateInventoryAsync(notification.CharacterId, cancellationToken);
    }
}