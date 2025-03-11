using MediatR;

namespace Application.UseCases.Characters.Events;
public record CharacterLevelUpEvent(Guid CharacterId, int Level) : INotification;