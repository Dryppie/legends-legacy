using Application.UseCases.Characters.Events;
using Domain.Components.Leveling;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;
public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    private readonly List<LevelTrigger> _triggers;

    public CharacterLevelUpEventHandler(List<LevelTrigger> triggers)
    {
        _triggers = triggers;
    }

    public Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        int newLevel = notification.Level;
        Guid characterId = notification.CharacterId;
        
        var validTriggers = _triggers.Where(t => t.Condition(newLevel));

        // Check each trigger
        foreach (var trigger in validTriggers)
        {
            // Fire the configured action
            trigger.Action(characterId);
        }
        // TODO: Temporary
        return Task.CompletedTask;
    }
}
