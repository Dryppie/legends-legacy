using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Characters.Events;
using Application.UseCases.Outbox;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    private readonly IGameEventOutbox _outbox;
    private readonly IEssenceSlotUnlockService _essenceSlotUnlocks;

    public CharacterLevelUpEventHandler(
        IGameEventOutbox outbox,
        IEssenceSlotUnlockService essenceSlotUnlocks)
    {
        _outbox = outbox;
        _essenceSlotUnlocks = essenceSlotUnlocks;
    }

    public async Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        // Essence attunement slots are now derived by IEssenceSlotUnlockService from character level.
        // No legacy EssenceSlot rows are created on level-up.
        //
        // The realtime CharacterLevelUp event is broadcast by RealtimeCharacterGameEventOutboxConsumer
        // instead of from here. This handler runs inside the command transaction, so publishing
        // directly raced the commit: clients received the level-up, immediately refetched derived
        // state such as essence attunement slots, and could still read the pre-level-up row.
        await _outbox.EnqueueAsync(
            GameEventTypes.CharacterLevelReached,
            new CharacterLevelReachedPayload(
                notification.CharacterId,
                notification.Level,
                notification.Experience,
                notification.ExperienceUntilNextLevel,
                _essenceSlotUnlocks.GetUnlockedSlotCount(notification.Level)),
            notification.CharacterId,
            null,
            cancellationToken);
    }
}
