using Application.UseCases.Characters.Events;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    public Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        // Essence attunement slots are now derived by IEssenceSlotUnlockService from character level.
        // No legacy EssenceSlot rows are created on level-up.
        return Task.CompletedTask;
    }
}
