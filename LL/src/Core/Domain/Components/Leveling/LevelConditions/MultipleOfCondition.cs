using Domain.Interfaces.Leveling;

namespace Domain.Components.Leveling.LevelConditions;
/// <summary>
/// Condition that checks if `level` is a multiple of `Divisor`.
/// Optional to check if `level` <= some MaxLevel, etc.
/// </summary>
public class MultipleOfCondition : ILevelCondition
{
    public int Divisor { get; set; }
    public int? MaxLevel { get; set; }  // optional

    public bool IsSatisfied(int level)
    {
        if (Divisor <= 0) return false;

        bool isMultiple = (level % Divisor == 0);
        if (MaxLevel.HasValue)
        {
            return isMultiple && level <= MaxLevel.Value;
        }
        return isMultiple;
    }
}
