using Domain.Models.Combat;

namespace Domain.Models.CharacterActions.Sessions;
public class CombatSession
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public CombatResult CombatResult { get; set; } = null!;
    public CombatSummary CombatSummary { get; set; } = null!;
}