using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Characters.Events;
using Domain.Components.Leveling;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;
public class CharacterLevelUpEventHandler : INotificationHandler<CharacterLevelUpEvent>
{
    //private readonly List<LevelTrigger> _triggers;
    private readonly IEssenceSlotService _essenceSlotService;

    public CharacterLevelUpEventHandler(IEssenceSlotService essenceSlotService)
    {
        //_triggers = LevelTriggerLoader.Instance.GetLevelTriggers();
        _essenceSlotService = essenceSlotService;
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

        if (newLevel % 5 == 0 && newLevel <= 100)
        {
            await _essenceSlotService.CreateEssenceSlotOnLevelUp(characterId, Domain.Models.Essences.EssenceSlots.SlotState.Active, cancellationToken);
        }

        //if (newLevel % 10 == 0 && newLevel <= 90)
        //{
        //    await _essenceSlotService.CreateEssenceSlotOnLevelUp(characterId, Domain.Models.Essences.EssenceSlots.SlotState.Reserved, cancellationToken);
        //}

        //if (newLevel % 10 == 0)
        //{

        //}
    }
}
