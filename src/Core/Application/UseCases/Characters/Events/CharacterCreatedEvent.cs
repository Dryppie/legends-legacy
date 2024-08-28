using MediatR;

namespace Application.UseCases.Characters.Events;
public record CharacterCreatedEvent(Guid CharacterId) : INotification;