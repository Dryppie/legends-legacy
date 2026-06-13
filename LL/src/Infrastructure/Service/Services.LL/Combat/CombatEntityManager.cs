using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Combat;

namespace Services.LL.Combat;
public class CombatEntityManager : ICombatEntityManager
{
    private readonly List<CombatEntity> _playerTeam = [];
    private readonly List<CombatEntity> _enemyTeam = [];

    public List<CombatEntity> PlayerTeam => _playerTeam;
    public List<CombatEntity> EnemyTeam => _enemyTeam;
    public List<CombatEntity> AllEntities => [.. _playerTeam, .. _enemyTeam];

    public void InitializeCombatEntityManager(List<CombatEntity> playerTeam, List<CombatEntity> enemyTeam)
    {
        _playerTeam.Clear();
        _enemyTeam.Clear();
        _playerTeam.AddRange(playerTeam);
        _enemyTeam.AddRange(enemyTeam);
        SetTriggerFilters();
    }

    private void SetTriggerFilters()
    {
        foreach (var entity in AllEntities)
        {
            foreach (var ability in entity.Abilities)
            {
                foreach (var trigger in ability.Definition.Triggers)
                {
                    foreach (var filter in trigger.Filters)
                    {
                        if (filter is SourceIsSelfFilter selfFilter)
                        {
                            selfFilter.SetOwner(entity);
                        }
                    }
                }
            }
        }
    }

    public void AddEntityToOwnTeam(CombatEntity self, CombatEntity entityToAdd)
    {
        if (_playerTeam.Contains(self))
        {
            _playerTeam.Add(entityToAdd);
        }
        else if (_enemyTeam.Contains(self))
        {
            _enemyTeam.Add(entityToAdd);
        }
        else
        {
            throw new InvalidOperationException("Caster not found on any team.");
        }
    }

    public void RemoveEntity(CombatEntity entity)
    {
        _playerTeam.Remove(entity);
        _enemyTeam.Remove(entity);
    }

    public List<CombatEntity> GetOpposingTeam(CombatEntity entity)
    {
        return _playerTeam.Contains(entity) ? _enemyTeam : _playerTeam;
    }

    public List<CombatEntity> GetOwnTeam(CombatEntity entity)
    {
        return _playerTeam.Contains(entity) ? _playerTeam : _enemyTeam;
    }

    public bool IsCombatActive()
    {
        // Check if at least one team still has alive entities
        bool playersAlive = _playerTeam.Any(e => e.IsAlive);
        bool enemiesAlive = _enemyTeam.Any(e => e.IsAlive);
        return playersAlive && enemiesAlive;
    }

    public List<CombatEntity> SelectTargets(CombatEntity actor, CombatTargeting targeting)
    {
        var enemyTeam = GetOpposingTeam(actor);
        var allyTeam = GetOwnTeam(actor);

        return TargetingManager.SelectTargets(targeting, actor, enemyTeam, allyTeam);
    }
}