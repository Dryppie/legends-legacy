using MediatR;

namespace Application.UseCases.Essences.Events;

public sealed record EssenceAbsorbedEvent(Guid CharacterId, string EssenceDefinitionId) : INotification;
