using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Services.LL.Interfaces;
public interface ICombatSetupService
{
    List<CombatEntity> CreateCombatEntities(List<Entity> entities);
    /// <summary>
    /// Appends prefix to Ids so combat creatures are unique in case you fight multiple of the same
    /// </summary>
    /// <param name="selectedCombatEnemyEntities"></param>
    void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities);
    Task PrepareEntitiesForCombat(List<CombatEntity> entities);
    List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities);
}
