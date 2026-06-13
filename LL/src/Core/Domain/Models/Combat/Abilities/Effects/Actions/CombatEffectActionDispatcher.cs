using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public static class CombatEffectActionDispatcher
{
    private static readonly IReadOnlyDictionary<string, ICombatEffectOperationHandler> Handlers =
        new ICombatEffectOperationHandler[]
        {
            new DamageEffectOperationHandler(),
            new RestoreResourceEffectOperationHandler(),
            new ModifyAttributeEffectOperationHandler(),
            new ApplyStatusEffectOperationHandler(),
            new RemoveStatusEffectOperationHandler(),
            new ModifyStatusEffectOperationHandler(),
            new CleanseEffectOperationHandler(),
            new SummonEffectOperationHandler(),
            new SelfDestructEffectOperationHandler(),
            new TriggerSecondaryEffectOperationHandler()
        }.ToDictionary(x => x.Operation, StringComparer.OrdinalIgnoreCase);

    public static void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext) =>
        GetHandler(action).Execute(action, effect, combatContext);

    public static void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext) =>
        GetHandler(action).OnExpire(action, effect, combatContext);

    private static ICombatEffectOperationHandler GetHandler(CombatEffectAction action) =>
        Handlers.TryGetValue(action.Operation, out var handler)
            ? handler
            : throw new NotSupportedException($"Combat effect operation '{action.Operation}' is not supported.");
}
