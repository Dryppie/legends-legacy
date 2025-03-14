using Application.UseCases.Characters.Events;
using Domain.Components.Leveling;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;
public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    private readonly List<LevelTrigger> _triggers;

    public CharacterLevelUpEventHandler()
    {
        //_triggers = LevelTriggerLoader.Instance.GetLevelTriggers();
    }

    public async Task Handle(CharacterLevelUpEvent notification, CancellationToken cancellationToken)
    {
        int newLevel = notification.Level;
        Guid characterId = notification.CharacterId;
        
        //var validTriggers = _triggers.Where(t => t.Condition.IsSatisfied(newLevel));

        //// Check each trigger
        //foreach (var trigger in validTriggers)
        //{
        //    // Fire the configured action
        //    await trigger.Action.Execute(characterId);
        //}

        if (newLevel % 10  == 0 && newLevel <= 100)
        {

        }

        if (newLevel % 30 == 90)
        {

        }

        //if (newLevel % 10 == 0)
        //{

        //}
    }
}
