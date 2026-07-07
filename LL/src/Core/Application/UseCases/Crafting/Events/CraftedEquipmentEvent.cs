using Domain.Models.Items.Equipments;
using MediatR;

namespace Application.UseCases.Crafting.Events;

public sealed record CraftedEquipmentEvent(
    Guid CharacterId,
    IReadOnlyCollection<EquipmentInstance> CraftedItems) : INotification;
