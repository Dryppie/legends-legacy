using Domain.Models.Combat;
using Services.LL.Interfaces;
using System.Collections.Concurrent;

namespace Services.LL.Combat.Stats;
public sealed class CombatStatsAggregator : ICombatStatsAggregator
{
    public IReadOnlyList<EntityStats> Aggregate(IEnumerable<CombatLogItem> log)
    {
        // 1) allocate work dictionaries
        var entityMap = new ConcurrentDictionary<string, WorkEntity>();

        foreach (var item in log)
        {
            // ----- entity context ------------------------------------------------
            var entity = entityMap.GetOrAdd(item.ActorId, static id => new WorkEntity(id));

            // ----- high-level stats ----------------------------------------------
            switch (item.EventType)
            {
                case EventType.Damage:
                    entity.DamageDone += item.Magnitude;
                    break;
                case EventType.Heal:
                    entity.HealingDone += item.Magnitude;
                    break;
                // add more global categories here
                case EventType.HealthRegeneration:
                    entity.HealthRegenerated += item.Magnitude;
                    break;
            }

            // ----- ability context -----------------------------------------------
            switch (item.EventType)
            {
                case EventType.AbilityUse:
                    if (string.IsNullOrWhiteSpace(item.Source))
                        break;

                    var usedAbility = entity.GetOrAddAbility(item.Source);
                    usedAbility.Uses++;
                    break;

                case EventType.Damage:
                case EventType.DamageOverTime:
                case EventType.DamageCrit:
                    if (string.IsNullOrWhiteSpace(item.Source))
                        break;

                    var damageAbility = entity.GetOrAddAbility(item.Source);
                    damageAbility.TotalDamage += item.Magnitude;
                    damageAbility.Hits++;
                    if (item.EventType == EventType.DamageCrit)
                        damageAbility.Crits++;
                    break;

                case EventType.Heal:
                case EventType.HealOverTime:
                case EventType.HealCrit:
                    if (string.IsNullOrWhiteSpace(item.Source))
                        break;

                    var healAbility = entity.GetOrAddAbility(item.Source);
                    healAbility.TotalHealing += item.Magnitude;
                    healAbility.Hits++;
                    if (item.EventType == EventType.HealCrit)
                        healAbility.Crits++;
                    break;

                case EventType.Summon:
                    if (string.IsNullOrWhiteSpace(item.Source))
                        break;

                    var summonAbility = entity.GetOrAddAbility(item.Source);
                    summonAbility.Summons++;
                    break;

                //case EventType.StatusEffect:
                //    ability.Stuns++;
                //    break;
            }

            // ----- target-side bookkeeping ---------------------------------------
            if (item.TargetId is { Length: > 0 })
            {
                var target = entityMap.GetOrAdd(item.TargetId, static id => new WorkEntity(id));
                if (item.EventType == EventType.Damage || item.EventType == EventType.DamageOverTime || item.EventType == EventType.DamageCrit)
                    target.DamageTaken += item.Magnitude;
                else if (item.EventType == EventType.Heal || item.EventType == EventType.HealOverTime || item.EventType == EventType.HealCrit)
                    target.HealingReceived += item.Magnitude;
            }
        }

        // 2) materialize immutable view models
        return entityMap.Values
                        .Select(e => e.ToImmutable())
                        .ToList()
                        .AsReadOnly();
    }
}

public sealed class WorkEntity
{
    public string Id { get; }
    public string Name => _firstEntityName ?? Id;
    public int DamageDone, DamageTaken, HealingDone, HealingReceived, HealthRegenerated;

    private readonly Dictionary<string, WorkAbility> _abilities = new(StringComparer.Ordinal);
    private string? _firstEntityName;

    public WorkEntity(string id) => Id = id;

    public WorkAbility GetOrAddAbility(string abilityName)
    {
        if (!_abilities.TryGetValue(abilityName, out var ability))
            _abilities[abilityName] = ability = new WorkAbility(abilityName);
        return ability;
    }

    public EntityStats ToImmutable() =>
        new(Id, Name, _abilities.Values
            .Select(a => a.ToImmutable())
            .OrderByDescending(a => Math.Max(a.TotalDamage, a.TotalHealing))
            .ToList(),
        DamageDone, DamageTaken, HealingDone, HealingReceived, HealthRegenerated);
}

public sealed class WorkAbility
{
    public string Name { get; }
    public int TotalDamage, TotalHealing, Uses, Hits, Crits, Summons, Stuns;

    public WorkAbility(string name) => Name = name;

    public AbilityStats ToImmutable() => new(Name, TotalDamage, TotalHealing, Uses, Hits, Crits, Summons, Stuns);
}
