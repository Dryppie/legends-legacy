using Domain.Models.Abilities.Statuses;

namespace Domain.Interfaces.Combat;
public interface IStatusDefinitionService
{
    bool TryGetById(string id, out StatusDefinition def);
    StatusDefinition GetById(string id);
    IReadOnlyCollection<StatusDefinition> GetAll();
}
