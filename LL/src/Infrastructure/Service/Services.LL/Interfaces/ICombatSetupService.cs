using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;
public interface ICombatSetupService
{
    List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities);
    List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area);
    /// <summary>
    /// Appends prefix to Ids so combat creatures are unique in case you fight multiple of the same
    /// </summary>
    /// <param name="selectedCombatEnemyEntities"></param>
    void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities);
    Task PrepareEntitiesForCombat(List<CombatEntity> entities);
    Task PrepareEntitiesForCombat(List<CombatEntity> entities, EssenceCombatActivity activity) =>
        PrepareEntitiesForCombat(entities);
    List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities);
}
