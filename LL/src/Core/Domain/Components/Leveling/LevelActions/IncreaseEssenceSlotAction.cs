using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling.LevelActions;

/// <summary>
/// Action: Increase an "essence slot" by some 1.
/// </summary>
public class IncreaseEssenceSlotAction : ILevelAction
{
    public async Task Execute(Guid characterId)
    {
        Console.WriteLine($"[Action] Increased {characterId}'s essence slots by 1. ");
    }
}