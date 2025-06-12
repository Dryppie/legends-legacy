using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Triggers.TriggerFilters;
public class AbilityIdFilter : ITriggerFilter
{
    public List<string> AllowedIds { get; set; } = [];

    public bool IsMatch(CombatEvent e) =>
        e.AbilityId != null && AllowedIds.Contains(e.AbilityId);

    public ITriggerFilter Clone() => new AbilityIdFilter { AllowedIds = [.. AllowedIds] };
}
