using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
public class StatusIdFilter : ITriggerFilter
{
    public List<string> StatusIds { get; set; } = [];

    public bool IsMatch(CombatEvent e) =>
        e.StatusId != null && StatusIds.Contains(e.StatusId);

    public ITriggerFilter Clone() => new StatusIdFilter { StatusIds = [.. StatusIds] };
}
