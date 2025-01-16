using Domain.Models.Abilities;
using Domain.Models.Combat;

namespace Services.LL.Combat;
public static class TargetingManager
{
    public static List<CombatEntity> SelectTargets(Targeting targeting, CombatEntity actor, List<CombatEntity> enemyTeam, List<CombatEntity> allies)
    {
        List<CombatEntity> targets = [];

        switch (targeting)
        {
            case Targeting.SingleEnemy:
                var enemyTarget = SelectTarget(enemyTeam);
                if (enemyTarget != null) targets.Add(enemyTarget);
                break;

            case Targeting.AllEnemies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;
            case Targeting.TwoEnemies:
                if (enemyTeam.Where(e => e.IsAlive).Count() >= 2)
                {
                    targets = enemyTeam.Where(e => e.IsAlive).Take(2).ToList();
                }
                else
                {
                    var enemyTargets = SelectTarget(enemyTeam);
                    if (enemyTargets != null) targets.Add(enemyTargets);
                }
                break;
            case Targeting.TwoAllies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;

            case Targeting.Self:
                targets.Add(actor);
                break;

            case Targeting.SingleAlly:
                var allyTarget = SelectTarget(allies);
                if (allyTarget != null) targets.Add(allyTarget);
                break;

            case Targeting.AllAllies:
                targets = allies.Where(a => a.IsAlive).ToList();
                break;

            default:
                throw new NotSupportedException($"Targeting type '{targeting}' is not supported.");
        }

        return targets;
    }

    private static CombatEntity? SelectTarget(List<CombatEntity> potentialTargets)
    {
        // Select a random alive target
        var aliveTargets = potentialTargets.Where(c => c.IsAlive).ToList();
        if (aliveTargets.Count == 0) return null;

        var random = new Random();
        int index = random.Next(aliveTargets.Count);
        return aliveTargets[index];
    }
}