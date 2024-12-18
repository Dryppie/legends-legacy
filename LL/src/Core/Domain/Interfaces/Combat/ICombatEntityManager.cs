using Domain.Models.Entities;

namespace Domain.Interfaces.Combat;
public interface ICombatEntityManager
{
    /// <summary>
    /// Returns a list of entities on the player's team.
    /// </summary>
    List<Entity> PlayerTeam { get; }

    /// <summary>
    /// Returns a list of entities on the enemy's team.
    /// </summary>
    List<Entity> EnemyTeam { get; }

    /// <summary>
    /// Returns all entities currently in combat.
    /// </summary>
    List<Entity> AllEntities { get; }

    /// <summary>
    /// Adds a new entity to ones team
    /// </summary>
    void AddEntityToOwnTeam(Entity self, Entity entityToAdd);

    /// <summary>
    /// Removes an entity from combat entirely.
    /// Useful if an entity dies, flees, or the effect that created it expires.
    /// </summary>
    void RemoveEntity(Entity entity);

    /// <summary>
    /// Returns the opposing team for a given entity.
    /// </summary>
    List<Entity> GetOpposingTeam(Entity entity);

    /// <summary>
    /// Returns the own team for a given entity.
    /// </summary>
    List<Entity> GetOwnTeam(Entity entity);

    /// <summary>
    /// Check if combat should continue or if a team has been wiped out.
    /// </summary>
    bool IsCombatActive();
}