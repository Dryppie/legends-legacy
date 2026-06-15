using Application.Interfaces.WebSockets;
using Application.UseCases.Characters.Events;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    private readonly IGameEventPublisher _eventPublisher;

    public CharacterLevelUpEventHandler(IGameEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        // Essence attunement slots are now derived by IEssenceSlotUnlockService from character level.
        // No legacy EssenceSlot rows are created on level-up.
        await _eventPublisher.PublishAsync(
            new Audience.Character(notification.CharacterId),
            new CharacterLevelUpMsg(
                notification.CharacterId,
                notification.Level,
                notification.Experience,
                notification.ExperienceUntilNextLevel));
    }
}
