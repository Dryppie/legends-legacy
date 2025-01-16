namespace Domain.Models.Combat;
public class BattleContext
{
    public List<CombatEntity> OwnTeam { get; set; } = [];
    public List<CombatEntity> EnemyTeam { get; set; } = [];

    public BattleContext(List<CombatEntity> ownTeam, List<CombatEntity> enemyTeam)
    {
        OwnTeam = ownTeam;
        EnemyTeam = enemyTeam;
    }
}