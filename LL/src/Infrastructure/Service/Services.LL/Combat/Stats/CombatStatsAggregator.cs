using Domain.Models.Combat;
using Services.LL.Interfaces;
using System.Collections.Concurrent;

namespace Services.LL.Combat.Stats;
public sealed class CombatStatsAggregator : ICombatStatsAggregator
{
    public IReadOnlyList<EntityStats> Aggregate(IEnumerable<CombatLogItem> log) =>
        Aggregate(log, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public IReadOnlyList<EntityStats> Aggregate(
        IEnumerable<CombatLogItem> log,
        IReadOnlyDictionary<string, string> teamsByEntityId)
    {
        // 1) allocate work dictionaries
        var entityMap = new ConcurrentDictionary<string, WorkEntity>();

        foreach (var item in log)
        {
            // ----- entity context ------------------------------------------------
            var entity = entityMap.GetOrAdd(item.ActorId, static id => new WorkEntity(id));
            var actorTeam = ResolveTeam(item.ActorId, teamsByEntityId);
            var targetTeam = ResolveTeam(item.TargetId, teamsByEntityId);
            var relationship = ResolveTargetRelationship(item.ActorId, actorTeam, item.TargetId, targetTeam);
            entity.SetTeam(actorTeam);
            var statsSource = string.IsNullOrWhiteSpace(item.StatsSource) ? item.Source : item.StatsSource;
            if (!string.IsNullOrWhiteSpace(statsSource)
                && (item.EventType == EventType.AbilityUse || item.CountsAsActivation))
            {
                entity.GetOrAddAbility(statsSource).Uses++;
            }

            // ----- high-level stats ----------------------------------------------
            switch (item.EventType)
            {
                case EventType.Damage:
                case EventType.DamageOverTime:
                case EventType.DamageCrit:
                case EventType.ReflectedDamage:
                    if (relationship == DamageTargetRelationship.Opponent)
                        entity.DamageDone += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Self)
                        entity.SelfDamageDone += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Ally)
                        entity.AlliedDamageDone += item.Magnitude;
                    break;
                case EventType.Heal:
                case EventType.HealOverTime:
                case EventType.HealCrit:
                    entity.HealingDone += item.Magnitude;
                    break;
                // add more global categories here
                case EventType.HealthRegeneration:
                    entity.HealthRegenerated += item.Magnitude;
                    break;
                case EventType.RestoreBarrier:
                    entity.BarrierGenerated += item.Magnitude;
                    break;
            }

            // ----- ability context -----------------------------------------------
            switch (item.EventType)
            {
                case EventType.AbilityUse:
                    break;

                case EventType.Damage:
                case EventType.DamageOverTime:
                case EventType.DamageCrit:
                case EventType.ReflectedDamage:
                    if (string.IsNullOrWhiteSpace(statsSource))
                        break;

                    var damageAbility = entity.GetOrAddAbility(statsSource);
                    if (relationship == DamageTargetRelationship.Opponent)
                        damageAbility.TotalDamage += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Self)
                        damageAbility.SelfDamage += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Ally)
                        damageAbility.AlliedDamage += item.Magnitude;
                    damageAbility.Hits++;
                    if (item.EventType == EventType.DamageCrit)
                        damageAbility.Crits++;
                    break;

                case EventType.Heal:
                case EventType.HealOverTime:
                case EventType.HealCrit:
                    if (string.IsNullOrWhiteSpace(statsSource))
                        break;

                    var healAbility = entity.GetOrAddAbility(statsSource);
                    healAbility.TotalHealing += item.Magnitude;
                    healAbility.Hits++;
                    if (item.EventType == EventType.HealCrit)
                        healAbility.Crits++;
                    break;

                case EventType.RestoreBarrier:
                    if (string.IsNullOrWhiteSpace(statsSource))
                        break;

                    entity.GetOrAddAbility(statsSource).TotalBarrier += item.Magnitude;
                    break;

                case EventType.Summon:
                    if (string.IsNullOrWhiteSpace(statsSource))
                        break;

                    var summonAbility = entity.GetOrAddAbility(statsSource);
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
                target.SetTeam(targetTeam);
                target.SetName(item.CombatEntity?.Name);
                if (item.EventType == EventType.Damage
                    || item.EventType == EventType.DamageOverTime
                    || item.EventType == EventType.DamageCrit
                    || item.EventType == EventType.ReflectedDamage)
                {
                    target.DamageBlocked += item.BarrierAbsorbed;
                    target.IncomingRawDamage += item.IncomingRawDamage;
                    target.TypedMitigationPrevented += item.TypedMitigationPrevented;
                    target.PhysicalMitigationPrevented += item.PhysicalMitigationPrevented;
                    target.MagicalMitigationPrevented += item.MagicalMitigationPrevented;
                    target.BlockPrevented += item.BlockPrevented;
                    target.DamageReductionPrevented += item.DamageReductionPrevented;
                    target.DamageAmplified += item.DamageAmplified;
                    target.FinalHealthDamage += item.FinalHealthDamage;
                    if (relationship == DamageTargetRelationship.Opponent)
                        target.DamageTaken += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Self)
                        target.SelfDamageTaken += item.Magnitude;
                    else if (relationship == DamageTargetRelationship.Ally)
                        target.AlliedDamageTaken += item.Magnitude;
                }
                else if (item.EventType == EventType.Miss)
                {
                    target.IncomingRawDamage += item.IncomingRawDamage;
                    target.AvoidedDamage += item.AvoidedDamage;
                    target.AvoidedAttacks++;
                }
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

    private static string ResolveTeam(string entityId, IReadOnlyDictionary<string, string> teamsByEntityId) =>
        !string.IsNullOrWhiteSpace(entityId) && teamsByEntityId.TryGetValue(entityId, out var team) ? team : string.Empty;

    private static DamageTargetRelationship ResolveTargetRelationship(
        string actorId,
        string actorTeam,
        string targetId,
        string targetTeam)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return DamageTargetRelationship.Opponent;

        if (actorId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            return DamageTargetRelationship.Self;

        return !string.IsNullOrWhiteSpace(actorTeam)
               && actorTeam.Equals(targetTeam, StringComparison.OrdinalIgnoreCase)
            ? DamageTargetRelationship.Ally
            : DamageTargetRelationship.Opponent;
    }

    private enum DamageTargetRelationship
    {
        Opponent,
        Self,
        Ally
    }
}

public sealed class WorkEntity
{
    public string Id { get; }
    public string Name => _firstEntityName ?? Id;
    public string Team { get; private set; } = string.Empty;
    public int DamageDone, DamageTaken, HealingDone, HealingReceived, HealthRegenerated;
    public int SelfDamageDone, SelfDamageTaken, AlliedDamageDone, AlliedDamageTaken;
    public int BarrierGenerated, DamageBlocked;
    public int IncomingRawDamage, AvoidedDamage, AvoidedAttacks;
    public int TypedMitigationPrevented, PhysicalMitigationPrevented, MagicalMitigationPrevented;
    public int BlockPrevented, DamageReductionPrevented;
    public int DamageAmplified, FinalHealthDamage;

    private readonly Dictionary<string, WorkAbility> _abilities = new(StringComparer.Ordinal);
    private string? _firstEntityName;

    public WorkEntity(string id) => Id = id;

    public WorkAbility GetOrAddAbility(string abilityName)
    {
        if (!_abilities.TryGetValue(abilityName, out var ability))
            _abilities[abilityName] = ability = new WorkAbility(abilityName);
        return ability;
    }

    public void SetTeam(string team)
    {
        if (!string.IsNullOrWhiteSpace(team) && string.IsNullOrWhiteSpace(Team))
            Team = team;
    }

    public void SetName(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(_firstEntityName))
            _firstEntityName = name;
    }

    public EntityStats ToImmutable() =>
        new(Id, Name, _abilities.Values
            .Select(a => a.ToImmutable())
            .OrderByDescending(a => Math.Max(
                Math.Max(a.TotalDamage, a.TotalHealing),
                Math.Max(a.TotalBarrier, Math.Max(a.SelfDamage, a.AlliedDamage))))
            .ToList(),
        DamageDone,
        DamageTaken,
        HealingDone,
        HealingReceived,
        HealthRegenerated,
        SelfDamageDone,
        SelfDamageTaken,
        AlliedDamageDone,
        AlliedDamageTaken,
        Team,
        BarrierGenerated,
        DamageBlocked,
        IncomingRawDamage,
        AvoidedDamage,
        AvoidedAttacks,
        TypedMitigationPrevented,
        PhysicalMitigationPrevented,
        MagicalMitigationPrevented,
        BlockPrevented,
        DamageReductionPrevented,
        DamageAmplified,
        FinalHealthDamage);
}

public sealed class WorkAbility
{
    public string Name { get; }
    public int TotalDamage, TotalHealing, Uses, Hits, Crits, Summons, Stuns, SelfDamage, AlliedDamage, TotalBarrier;

    public WorkAbility(string name) => Name = name;

    public AbilityStats ToImmutable() => new(Name, TotalDamage, TotalHealing, Uses, Hits, Crits, Summons, Stuns, SelfDamage, AlliedDamage, TotalBarrier);
}
