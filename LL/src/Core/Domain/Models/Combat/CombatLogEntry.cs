namespace Domain.Models.Combat;
// One line per effect that matters for stats
public readonly record struct CombatLogEntry(
    Guid SourceId,
    string SourceName,
    EventType EventType,     // Damage, Heal, Miss, etc.
    int Amount,           // Damage or healing dealt
    bool IsCrit);