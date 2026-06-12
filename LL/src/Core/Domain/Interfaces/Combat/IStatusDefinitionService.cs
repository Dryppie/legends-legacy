using Domain.Models.Combat.Abilities.Statuses;

namespace Domain.Interfaces.Combat;
public interface IStatusDefinitionService
{
    bool TryGetById(string id, out StatusDefinition def);
    IReadOnlyCollection<StatusDefinition> GetAll();
}
