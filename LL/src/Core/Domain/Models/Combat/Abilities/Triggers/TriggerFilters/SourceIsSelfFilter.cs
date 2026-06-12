using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
public class SourceIsSelfFilter : ITriggerFilter
{
    private CombatEntity? _owner;

    public SourceIsSelfFilter(CombatEntity? owner) => _owner = owner;

    public void SetOwner(CombatEntity owner) => _owner = owner;

    public bool IsMatch(CombatEvent e) => _owner != null && e.Source != null && e.Source == _owner;

    public ITriggerFilter Clone() => new SourceIsSelfFilter(_owner) { _owner = _owner?.DeepCloneForEncounter() };
}