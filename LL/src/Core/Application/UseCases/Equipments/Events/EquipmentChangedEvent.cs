using MediatR;

namespace Application.UseCases.Equipments.Events;

public sealed record EquipmentChangedEvent(Guid CharacterId) : INotification;
