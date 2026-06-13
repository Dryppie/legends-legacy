using Domain.Models.Combat.Abilities;
using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatEntityManager
{
    /// <summary>
    /// Returns a list of entities on the player's team.
    /// </summary>
    List<CombatEntity> PlayerTeam { get; }

    /// <summary>
    /// Returns a list of entities on the enemy's team.
    /// </summary>
    List<CombatEntity> EnemyTeam { get; }

    /// <summary>
    /// Returns all entities currently in combat.
    /// </summary>
    List<CombatEntity> AllEntities { get; }

    void InitializeCombatEntityManager(List<CombatEntity> playerTeam, List<CombatEntity> enemyTeam);

    /// <summary>
    /// Adds a new entity to ones team
    /// </summary>
    void AddEntityToOwnTeam(CombatEntity self, CombatEntity entityToAdd);

    /// <summary>
    /// Removes an entity from combat entirely.
    /// Useful if an entity dies, flees, or the effect that created it expires.
    /// </summary>
    void RemoveEntity(CombatEntity entity);

    /// <summary>
    /// Returns the opposing team for a given entity.
    /// </summary>
    List<CombatEntity> GetOpposingTeam(CombatEntity entity);

    /// <summary>
    /// Returns the own team for a given entity.
    /// </summary>
    List<CombatEntity> GetOwnTeam(CombatEntity entity);

    /// <summary>
    /// Check if combat should continue or if a team has been wiped out.
    /// </summary>
    bool IsCombatActive();

    /// <summary>
    /// This makes it such that only EntityManager selects targets for abilities, effects, etc.
    /// </summary>
    /// <param name="actor"></param>
    /// <param name="targeting"></param>
    /// <returns></returns>
    List<CombatEntity> SelectTargets(CombatEntity actor, CombatTargeting targeting);
}