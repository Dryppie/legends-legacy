namespace Domain.Models.Combat.Abilities;
public enum CombatTargeting
{
    None,
    CauseOfTrigger,
    AttackedEnemy,
    Self,
    SingleEnemy,
    SingleAlly,
    TwoEnemies,
    TwoAllies,
    SingleDeadEnemy,
    SingleDeadAlly,
    SingleRandomEnemy,
    SingleRandomAlly,
    SingleEnemyLowestHealth,
    SingleAllyLowestHealth,
    AllEnemies,
    AllAllies,
    YourTeam,
    AllyHighestMaxHealth,
    EveryoneButYou,
}
