using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public class CombatantStatusCondition : ICondition
{
    private readonly bool _useSource;
    private readonly string _statusId;

    public CombatantStatusCondition(bool useSource, string statusId)
    {
        _useSource = useSource;
        _statusId = statusId;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var entity = _useSource ? source : target;
        if (Enum.TryParse<StatusEffectType>(_statusId, ignoreCase: true, out var statusEffect) && entity.StatusEffects.ContainsKey(statusEffect))
            return true;

        return entity.Statuses.Any(x => x.Definition.Id.Equals(_statusId, StringComparison.OrdinalIgnoreCase));
    }

    public void PerformCondition(CombatEntity target)
    {
    }

    public ICondition Clone() => new CombatantStatusCondition(_useSource, _statusId);
}
