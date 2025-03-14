using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling.LevelActions;

/// <summary>
/// Action: Increase an "essence reserve slot" by some 1.
/// </summary>
public class IncreaseEssenceReserveSlotAction : ILevelAction
{
    public async Task Execute(Guid characterId)
    {
        Console.WriteLine($"[Action] Increased {characterId}'s essence reserved slots by 1. ");
    }
}
