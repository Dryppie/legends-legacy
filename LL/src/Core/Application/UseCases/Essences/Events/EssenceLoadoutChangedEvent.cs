using MediatR;

namespace Application.UseCases.Essences.Events;

public sealed record EssenceLoadoutChangedEvent(
    Guid CharacterId,
    IReadOnlyCollection<Guid> AttunedPlayerEssenceIds) : INotification;
