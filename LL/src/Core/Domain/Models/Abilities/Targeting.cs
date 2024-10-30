namespace Domain.Models.Abilities;
public enum Targeting
{
    None,
    Self,
    SingleEnemy,
    SingleAlly,
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
