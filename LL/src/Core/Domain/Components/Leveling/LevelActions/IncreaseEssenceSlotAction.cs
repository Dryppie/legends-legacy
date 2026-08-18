using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling.LevelActions;

/// <summary>
/// Action: Increase an "essence slot" by some 1.
/// </summary>
public class IncreaseEssenceSlotAction : ILevelAction
{
    public Task Execute(Guid characterId) => Task.CompletedTask;
}
