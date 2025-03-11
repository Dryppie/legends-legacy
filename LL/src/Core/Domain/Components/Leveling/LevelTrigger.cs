namespace Domain.Components.Leveling;
public class LevelTrigger
{
    /// <summary>
    /// Condition to check if this trigger should fire for a given level.
    /// Example: level => (level % 10 == 0)
    /// </summary>
    public Func<int, bool> Condition { get; set; }

    /// <summary>
    /// The action to perform on the Character when the condition is met.
    /// Example: character => { /* Give bonus items, etc. */ }
    /// </summary>
    public Action<Guid> Action { get; set; }
}