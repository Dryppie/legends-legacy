using Domain.Models.Abilities.Effects;
using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatContext
{
    CombatResult InstantiateAndRunCombat(List<CombatEntity> playerEntities, List<CombatEntity> enemyEntities);
    public ICombatEntityManager EntityManager { get; set; }
    public ICombatEffectManager EffectManager { get; set; }
    public ICombatInteractionManager InteractionManager { get; set; }
    public IStatusDefinitionService StatusDefinitionService { get; set; }
    public ICombatEventBus EventBus { get; set; }
    int CurrentTime { get; }
    void LogEffectExecution(EffectContext context, SimpleCombatEntity? combatEntity = null);
}