namespace Domain.Models.Combat;
public sealed class EntityStatsDictionary
{
    private readonly Dictionary<string, AbilityStats> _byAbility = new();
    public IReadOnlyDictionary<string, AbilityStats> ByAbility => _byAbility;

    public void Apply(CombatLogEntry entry)
    {
        if (!_byAbility.TryGetValue(entry.SourceName, out var stats))
        {
            stats = new AbilityStats();
            _byAbility[entry.SourceName] = stats;
        }
        stats.Apply(entry);
    }
}
