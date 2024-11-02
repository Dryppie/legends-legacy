namespace Domain.Models.Abilities;
public enum Targeting
{
    None,
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
}
