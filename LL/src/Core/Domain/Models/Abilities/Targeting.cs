namespace Domain.Models.Abilities;
public enum Targeting
{
    None,
    CauseOfTrigger,
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
    AllAlliesAndSelf,
    AllyHighestMaxHealth,
    EveryoneButYou,
}
