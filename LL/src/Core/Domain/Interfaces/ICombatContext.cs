using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Interfaces;
public interface ICombatContext
{
    void AddEntityToTeam(Entity caster, Entity newEntity);
    void RemoveEntityFromTeam(Entity summonedEntity);
}