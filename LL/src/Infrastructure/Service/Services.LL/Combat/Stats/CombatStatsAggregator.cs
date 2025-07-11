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
            }

            // ----- ability context -----------------------------------------------
            var ability = entity.GetOrAddAbility(item.Source); // or item.Source
            switch (item.EventType)
            {
                case EventType.Damage:
                    ability.TotalDamage += item.Magnitude;
                    ability.Hits++;
                    break;

                case EventType.Heal:
                    ability.TotalHealing += item.Magnitude;
                    ability.Hits++;
                    break;

                case EventType.Summon:
                    ability.Summons++;
                    break;

                //case EventType.StatusEffect:
                //    ability.Stuns++;
                //    break;
            }

            if (item.EventType == EventType.DamageCrit || item.EventType == EventType.HealCrit)
            {
                ability.Crits++;
            }

            // ----- target-side bookkeeping ---------------------------------------
            if (item.TargetId is { Length: > 0 })
            {
                var target = entityMap.GetOrAdd(item.TargetId, static id => new WorkEntity(id));
                if (item.EventType == EventType.Damage)
                    target.DamageTaken += item.Magnitude;
                else if (item.EventType == EventType.Heal)
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
    public int DamageDone, DamageTaken, HealingDone, HealingReceived;

    private readonly Dictionary<string, WorkAbility> _abilities = new(StringComparer.Ordinal);
    private string? _firstEntityName;

    public WorkEntity(string id) => Id = id;

    public WorkAbility GetOrAddAbility(string abilityName)
    {
        if (!_abilities.TryGetValue(abilityName, out var a))
            _abilities[abilityName] = a = new WorkAbility(abilityName);
        return a;
    }

    public EntityStats ToImmutable() =>
        new(Id, Name, _abilities.Values
            .Select(a => a.ToImmutable())
            .OrderByDescending(a => Math.Max(a.TotalDamage, a.TotalHealing))
            .ToList(),
        DamageDone, DamageTaken, HealingDone, HealingReceived);
}

public sealed class WorkAbility
{
    public string Name { get; }
    public int TotalDamage, TotalHealing, Hits, Crits, Summons, Stuns;

    public WorkAbility(string name) => Name = name;

    public AbilityStats ToImmutable() => new(Name, TotalDamage, TotalHealing, Hits, Crits, Summons, Stuns);
}