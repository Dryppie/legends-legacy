using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling;
public class LevelTrigger
{
    /// <summary>
    /// Condition to check if this trigger should fire for a given level.
    /// </summary>
    public ILevelCondition Condition { get; set; }

    /// <summary>
    /// The action to perform on the Character when the condition is met.
    /// </summary>
    public ILevelAction Action { get; set; }
}