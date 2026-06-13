using Domain.Models.Combat.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Services.LL.Combat;
public static class TargetingManager
{
    public static List<CombatEntity> SelectTargets(CombatTargeting targeting, CombatEntity actor, List<CombatEntity> enemyTeam, List<CombatEntity> allies)
    {
        List<CombatEntity> targets = [];

        switch (targeting)
        {
            case CombatTargeting.SingleEnemy:
            case CombatTargeting.SingleRandomEnemy:
                var enemyTarget = SelectTarget(enemyTeam);
                if (enemyTarget != null) targets.Add(enemyTarget);
                break;

            case CombatTargeting.AllEnemies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;

            case CombatTargeting.TwoEnemies:
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

            case CombatTargeting.TwoAllies:
                if (allies.Where(e => e.IsAlive).Count() >= 2)
                {
                    targets = allies.Where(e => e.IsAlive).Take(2).ToList();
                }
                else
                {
                    var allyTargets = SelectTarget(allies);
                    if (allyTargets != null) targets.Add(allyTargets);
                }
                break;

            case CombatTargeting.Self:
                targets.Add(actor);
                break;

            case CombatTargeting.SingleAlly:
            case CombatTargeting.SingleRandomAlly:
                var allyTarget = SelectTarget(allies);
                if (allyTarget != null) targets.Add(allyTarget);
                break;

            case CombatTargeting.AllAllies:
                targets = allies.Where(a => a.IsAlive && !a.Id.Equals(actor.Id)).ToList();
                break;

            case CombatTargeting.SingleEnemyLowestHealth:
                var lowestHealthEnemy = enemyTeam.Where(e => e.IsAlive).MinBy(e => e.GetCurrentHealthValue());
                if (lowestHealthEnemy != null) targets.Add(lowestHealthEnemy);
                break;

            case CombatTargeting.SingleAllyLowestHealth:
                var lowestHealthAlly = allies.Where(e => e.IsAlive).MinBy(e => e.GetCurrentHealthValue());
                if (lowestHealthAlly != null) targets.Add(lowestHealthAlly);
                break;

            case CombatTargeting.AllyHighestMaxHealth:
                var maxHealthAlly = allies.MaxBy(a => a.GetAttributeValue(AttributeType.MaxHealth));
                if (maxHealthAlly != null) targets.Add(maxHealthAlly);
                break;
            case CombatTargeting.EveryoneButYou:
                var allEnemies = enemyTeam.Where(e => e.IsAlive);
                var allAllies = allies.Where(a => a.IsAlive && !a.Id.Equals(actor.Id)).ToList();
                targets.AddRange([.. allEnemies, .. allAllies]);
                break;
            case CombatTargeting.YourTeam:
                var yourTeam = allies.Where(a => a.IsAlive).ToList();
                targets.AddRange(yourTeam);
                break;
            case CombatTargeting.SummonedAllies:
                targets.AddRange(allies.Where(a => a.IsAlive && a.IsSummoned));
                break;
            case CombatTargeting.NonSummonedAllies:
                targets.AddRange(allies.Where(a => a.IsAlive && !a.IsSummoned));
                break;
            default:
                throw new NotSupportedException($"Targeting type '{targeting}' is not supported.");
        }

        return targets;
    }

    public static CombatEntity? SelectTarget(List<CombatEntity> potentialTargets)
    {
        // Select a random alive target
        var aliveTargets = potentialTargets.Where(c => c.IsAlive).ToList();
        if (aliveTargets.Count == 0) return null;

        var random = new Random();
        int index = random.Next(aliveTargets.Count);
        return aliveTargets[index];
    }
}
