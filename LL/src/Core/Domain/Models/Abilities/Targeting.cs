namespace Domain.Models.Abilities;
public enum Targeting
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
