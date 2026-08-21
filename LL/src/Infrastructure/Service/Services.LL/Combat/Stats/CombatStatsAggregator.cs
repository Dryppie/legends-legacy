using Domain.Models.Combat;
using Domain.Models.Damages;
using Services.LL.Interfaces;

namespace Services.LL.Combat.Stats;

public sealed class CombatStatsAggregator : ICombatStatsAggregator
{
    public IReadOnlyList<EntityStats> Aggregate(IEnumerable<CombatLogItem> log) =>
        Aggregate(log, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public IReadOnlyList<EntityStats> Aggregate(
        IEnumerable<CombatLogItem> log,
        IReadOnlyDictionary<string, string> teamsByEntityId)
    {
        var accumulator = new CombatStatsAccumulator();
        accumulator.AddRange(log, teamsByEntityId);
        return accumulator.Snapshot();
    }
}

public sealed class CombatStatsAccumulator
{
    private readonly Dictionary<string, WorkEntity> _entityMap = new(StringComparer.OrdinalIgnoreCase);

    public void AddRange(
        IEnumerable<CombatLogItem> log,
        IReadOnlyDictionary<string, string> teamsByEntityId)
    {
        foreach (var item in log)
        {
            Add(
                item.Source,
                item.StatsSource,
                item.CountsAsActivation,
                item.ActorId,
                ResolveTeam(item.ActorId, teamsByEntityId),
                item.TargetId,
                ResolveTeam(item.TargetId, teamsByEntityId),
                item.CombatEntity?.Name,
                item.EventType,
                item.Magnitude,
                item.BarrierAbsorbed,
                item.IncomingRawDamage,
                item.AvoidedDamage,
                item.TypedMitigationPrevented,
                item.PhysicalMitigationPrevented,
                item.MagicalMitigationPrevented,
                item.BlockPrevented,
                item.DamageReductionPrevented,
                item.DamageAmplified,
                item.FinalHealthDamage,
                item.DamageType,
                item.DamageRedirectedTo,
                item.DamageRedirectedAway,
                item.CountsAsTargetedAttack);
        }
    }

    public void Add(
        string source,
        string statsSource,
        bool countsAsActivation,
        string actorId,
        string actorTeam,
        string targetId,
        string targetTeam,
        string? targetName,
        EventType eventType,
        int magnitude,
        int barrierAbsorbed = 0,
        int incomingRawDamage = 0,
        int avoidedDamage = 0,
        int typedMitigationPrevented = 0,
        int physicalMitigationPrevented = 0,
        int magicalMitigationPrevented = 0,
        int blockPrevented = 0,
        int damageReductionPrevented = 0,
        int damageAmplified = 0,
        int finalHealthDamage = 0,
        DamageType damageType = DamageType.None,
        int damageRedirectedTo = 0,
        int damageRedirectedAway = 0,
        bool countsAsTargetedAttack = false)
    {
        // ----- entity context ------------------------------------------------
        var entity = GetOrAddEntity(actorId);
        var relationship = ResolveTargetRelationship(actorId, actorTeam, targetId, targetTeam);
        entity.SetTeam(actorTeam);
        statsSource = string.IsNullOrWhiteSpace(statsSource) ? source : statsSource;
        if (!string.IsNullOrWhiteSpace(statsSource)
            && (eventType == EventType.AbilityUse || countsAsActivation))
        {
            entity.GetOrAddAbility(statsSource).Uses++;
        }

        // ----- high-level stats ----------------------------------------------
        switch (eventType)
        {
            case EventType.Damage:
            case EventType.DamageOverTime:
            case EventType.DamageCrit:
            case EventType.ReflectedDamage:
                if (relationship == DamageTargetRelationship.Opponent)
                    entity.DamageDone += magnitude;
                else if (relationship == DamageTargetRelationship.Self)
                    entity.SelfDamageDone += magnitude;
                else if (relationship == DamageTargetRelationship.Ally)
                    entity.AlliedDamageDone += magnitude;
                break;
            case EventType.Heal:
            case EventType.HealOverTime:
            case EventType.HealCrit:
                entity.HealingDone += magnitude;
                break;
            // add more global categories here
            case EventType.HealthRegeneration:
                entity.HealthRegenerated += magnitude;
                break;
            case EventType.RestoreBarrier:
                entity.BarrierGenerated += magnitude;
                break;
            case EventType.StaggerApplied:
                entity.StaggerContributed += magnitude;
                break;
            case EventType.StaggerBroken:
                entity.StaggerBreaks++;
                break;
        }

        // ----- ability context -----------------------------------------------
        switch (eventType)
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
                {
                    damageAbility.TotalDamage += magnitude;
                    damageAbility.AddDamage(damageType, magnitude);
                }
                else if (relationship == DamageTargetRelationship.Self)
                    damageAbility.SelfDamage += magnitude;
                else if (relationship == DamageTargetRelationship.Ally)
                    damageAbility.AlliedDamage += magnitude;
                damageAbility.Hits++;
                if (eventType == EventType.DamageCrit)
                    damageAbility.Crits++;
                break;

            case EventType.Heal:
            case EventType.HealOverTime:
            case EventType.HealCrit:
                if (string.IsNullOrWhiteSpace(statsSource))
                    break;

                var healAbility = entity.GetOrAddAbility(statsSource);
                healAbility.TotalHealing += magnitude;
                healAbility.Hits++;
                if (eventType == EventType.HealCrit)
                    healAbility.Crits++;
                break;

            case EventType.RestoreBarrier:
                if (string.IsNullOrWhiteSpace(statsSource))
                    break;

                entity.GetOrAddAbility(statsSource).TotalBarrier += magnitude;
                break;

            case EventType.Summon:
                if (string.IsNullOrWhiteSpace(statsSource))
                    break;

                var summonAbility = entity.GetOrAddAbility(statsSource);
                summonAbility.Summons++;
                break;

            case EventType.StaggerApplied:
                if (!string.IsNullOrWhiteSpace(statsSource))
                    entity.GetOrAddAbility(statsSource).TotalStagger += magnitude;
                break;

            case EventType.StaggerBroken:
                if (!string.IsNullOrWhiteSpace(statsSource))
                    entity.GetOrAddAbility(statsSource).StaggerBreaks++;
                break;

                //case EventType.StatusEffect:
                //    ability.Stuns++;
                //    break;
        }

        // ----- target-side bookkeeping ---------------------------------------
        if (targetId is { Length: > 0 })
        {
            var target = GetOrAddEntity(targetId);
            target.SetTeam(targetTeam);
            target.SetName(targetName);
            if (eventType == EventType.Damage
                || eventType == EventType.DamageOverTime
                || eventType == EventType.DamageCrit
                || eventType == EventType.ReflectedDamage)
            {
                target.DamageBlocked += barrierAbsorbed;
                target.IncomingRawDamage += incomingRawDamage;
                target.TypedMitigationPrevented += typedMitigationPrevented;
                target.PhysicalMitigationPrevented += physicalMitigationPrevented;
                target.MagicalMitigationPrevented += magicalMitigationPrevented;
                target.BlockPrevented += blockPrevented;
                target.DamageReductionPrevented += damageReductionPrevented;
                target.DamageAmplified += damageAmplified;
                target.FinalHealthDamage += finalHealthDamage;
                target.DamageRedirectedTo += damageRedirectedTo;
                target.DamageRedirectedAway += damageRedirectedAway;
                if (countsAsTargetedAttack)
                    target.TargetedAttacks++;
                if (relationship == DamageTargetRelationship.Opponent)
                    target.DamageTaken += magnitude;
                else if (relationship == DamageTargetRelationship.Self)
                    target.SelfDamageTaken += magnitude;
                else if (relationship == DamageTargetRelationship.Ally)
                    target.AlliedDamageTaken += magnitude;
            }
            else if (eventType == EventType.Miss)
            {
                target.IncomingRawDamage += incomingRawDamage;
                target.AvoidedDamage += avoidedDamage;
                target.AvoidedAttacks++;
                if (countsAsTargetedAttack)
                    target.TargetedAttacks++;
            }
            else if (eventType == EventType.Heal || eventType == EventType.HealOverTime || eventType == EventType.HealCrit)
                target.HealingReceived += magnitude;
        }
    }

    public IReadOnlyList<EntityStats> Snapshot() =>
        _entityMap.Values
            .Select(e => e.ToImmutable())
            .ToList()
            .AsReadOnly();

    private WorkEntity GetOrAddEntity(string entityId)
    {
        if (!_entityMap.TryGetValue(entityId, out var entity))
            _entityMap[entityId] = entity = new WorkEntity(entityId);
        return entity;
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
    public int DamageRedirectedTo, DamageRedirectedAway, TargetedAttacks;
    public int StaggerContributed, StaggerBreaks;

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
        FinalHealthDamage,
        DamageRedirectedTo: DamageRedirectedTo,
        DamageRedirectedAway: DamageRedirectedAway,
        TargetedAttacks: TargetedAttacks,
        StaggerContributed: StaggerContributed,
        StaggerBreaks: StaggerBreaks);
}

public sealed class WorkAbility
{
    public string Name { get; }
    public int TotalDamage, TotalHealing, Uses, Hits, Crits, Summons, Stuns, SelfDamage, AlliedDamage, TotalBarrier;
    public int TotalStagger, StaggerBreaks;
    private readonly Dictionary<DamageType, int> _damageByType = [];

    public WorkAbility(string name) => Name = name;

    public void AddDamage(DamageType damageType, int damage)
    {
        if (damage <= 0)
            return;

        _damageByType[damageType] = _damageByType.GetValueOrDefault(damageType) + damage;
    }

    public AbilityStats ToImmutable() => new(
        Name,
        TotalDamage,
        TotalHealing,
        Uses,
        Hits,
        Crits,
        Summons,
        Stuns,
        SelfDamage,
        AlliedDamage,
        TotalBarrier,
        _damageByType
            .OrderBy(entry => entry.Key)
            .Select(entry => new AbilityDamageTypeStats(entry.Key, entry.Value))
            .ToList(),
        TotalStagger: TotalStagger,
        StaggerBreaks: StaggerBreaks);
}
