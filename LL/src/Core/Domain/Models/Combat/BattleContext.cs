using Domain.Models.Entities;

namespace Domain.Models.Combat;
public class BattleContext
{
    public List<Entity> OwnTeam { get; set; } = [];
    public List<Entity> EnemyTeam { get; set; } = [];

    public BattleContext(List<Entity> ownTeam, List<Entity> enemyTeam)
    {
        OwnTeam = ownTeam;
        EnemyTeam = enemyTeam;
    }
}