using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution;

public enum CombatObjectivePolicy
{
    Elimination = 1,
    ObjectiveCompletion = 2,
    Survival = 3
}

public static class CombatObjectiveEvaluator
{
    public static BattleOutcome Evaluate(
        CombatObjectivePolicy policy,
        BattleOutcome engineOutcome,
        bool objectiveSatisfied) => policy switch
    {
        CombatObjectivePolicy.Elimination => engineOutcome,
        CombatObjectivePolicy.ObjectiveCompletion or CombatObjectivePolicy.Survival
            when objectiveSatisfied => BattleOutcome.Victory,
        CombatObjectivePolicy.ObjectiveCompletion or CombatObjectivePolicy.Survival => engineOutcome,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown combat objective policy.")
    };
}
