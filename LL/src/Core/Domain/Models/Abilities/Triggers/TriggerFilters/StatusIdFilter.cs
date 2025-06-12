using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Triggers.TriggerFilters;
public class StatusIdFilter : ITriggerFilter
{
    public List<string> StatusIds { get; set; } = [];

    public bool IsMatch(CombatEvent e) =>
        e.StatusId != null && StatusIds.Contains(e.StatusId);

    public ITriggerFilter Clone() => new StatusIdFilter { StatusIds = [.. StatusIds] };
}
