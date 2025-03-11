using Domain.Models.Inventories;
using MediatR;

namespace Application.UseCases.Inventories.Events;
public record LootGeneratedEvent(Guid CharacterId, List<InventoryItem> Loot) : INotification;