using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public sealed partial class FastCombatEngine
{
    private long _rootAction;
    private long _nextRootAction;
    private decimal _eventProcCoefficient = 1m;
    private bool _periodicProcEvent;
    private readonly HashSet<(object Listener, CompiledTrigger Trigger, string? Target, int Tick)> _procOpportunities = [];

    private ProcFrame BeginRootAction()
    {
        var frame = new ProcFrame(this, _rootAction, _eventProcCoefficient, _periodicProcEvent);
        if (_rootAction == 0)
        {
            _rootAction = ++_nextRootAction;
            _procOpportunities.Clear();
        }
        return frame;
    }

    private ProcFrame BeginEffectProcs(RuntimeCombatant source, CompiledEffect? effect, bool periodic = false, bool secondary = false)
    {
        var frame = BeginRootAction();
        if (!source.UsesCurrentAttributeRules) return frame;
        _periodicProcEvent = periodic || effect is not null && IsPeriodicEffect(effect);
        _eventProcCoefficient = (secondary || effect?.AbilityKind == AbilitySpecKind.Passive)
            && effect?.AllowSecondaryProcs != true ? 0m : effect?.ProcCoefficient ?? (periodic ? 0m : _eventProcCoefficient);
        return frame;
    }

    private bool CanReceiveProc(object listener, RuntimeCombatant owner, CompiledTrigger trigger, CombatEvent combatEvent,
        bool ownActive = false)
    {
        if (!owner.UsesCurrentAttributeRules || ownActive || !IsProcEvent(combatEvent.Event)) return true;
        if (_eventProcCoefficient <= 0 || _periodicProcEvent && !trigger.AllowPeriodicProcs) return false;
        var key = (listener, trigger, trigger.ProcScope == ProcScope.PerTarget ? combatEvent.Target?.Id : null,
            trigger.ProcScope == ProcScope.PerTick ? _currentTick : 0);
        if (!_procOpportunities.Add(key)) return false;
        // Exactly one coefficient trial for the event's listener opportunity; never scales damage.
        return _eventProcCoefficient >= 1m || _random.NextDouble() < (double)_eventProcCoefficient;
    }

    private static bool IsProcEvent(AbilityTriggerEvent trigger) => trigger is
        AbilityTriggerEvent.OnAbilityUsed or AbilityTriggerEvent.OnBasicAttack or AbilityTriggerEvent.OnHit
        or AbilityTriggerEvent.OnDamageDealt or AbilityTriggerEvent.OnDamaged or AbilityTriggerEvent.OnAttacked
        or AbilityTriggerEvent.OnMeleeAttack or AbilityTriggerEvent.OnRangedAttack
        or AbilityTriggerEvent.OnMeleeAttacked or AbilityTriggerEvent.OnRangedAttacked
        or AbilityTriggerEvent.OnHeal or AbilityTriggerEvent.OnHealed or AbilityTriggerEvent.OnLifestealHeal
        or AbilityTriggerEvent.OnDirectHealApplied or AbilityTriggerEvent.OnEnemyHealed;

    private readonly struct ProcFrame(FastCombatEngine engine, long root, decimal coefficient, bool periodic) : IDisposable
    {
        public void Dispose()
        {
            engine._rootAction = root;
            engine._eventProcCoefficient = coefficient;
            engine._periodicProcEvent = periodic;
        }
    }
}
