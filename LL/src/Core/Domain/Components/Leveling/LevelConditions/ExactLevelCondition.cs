using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling.LevelConditions;
/// <summary>
/// Condition that checks if `level` is exactly equal to a given value.
/// </summary>
public class ExactLevelCondition : ILevelCondition
{
    public int Value { get; set; }

    public bool IsSatisfied(int level) => (level == Value);
}
