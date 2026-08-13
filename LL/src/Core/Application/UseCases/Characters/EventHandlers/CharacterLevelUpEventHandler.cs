using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.WebSockets;
using Application.UseCases.Characters.Events;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IGameEventOutbox _outbox;
    private readonly IEssenceSlotUnlockService _essenceSlotUnlocks;

    public CharacterLevelUpEventHandler(
        IGameEventPublisher eventPublisher,
        IGameEventOutbox outbox,
        IEssenceSlotUnlockService essenceSlotUnlocks)
    {
        _eventPublisher = eventPublisher;
        _outbox = outbox;
        _essenceSlotUnlocks = essenceSlotUnlocks;
    }

    public async Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        // Essence attunement slots are now derived by IEssenceSlotUnlockService from character level.
        // No legacy EssenceSlot rows are created on level-up.
        await _outbox.EnqueueAsync(
            GameEventTypes.CharacterLevelReached,
            new CharacterLevelReachedPayload(
                notification.CharacterId,
                notification.Level),
            notification.CharacterId,
            null,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Character(notification.CharacterId),
            new CharacterLevelUpMsg(
                notification.CharacterId,
                notification.Level,
                notification.Experience,
                notification.ExperienceUntilNextLevel,
                _essenceSlotUnlocks.GetUnlockedSlotCount(notification.Level)));
    }
}
