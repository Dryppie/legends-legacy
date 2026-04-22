using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed class IdleCombatTemplateCatalog
{
    public IdleCombatTemplateCatalog(
        IReadOnlyDictionary<Guid, Entity> sourceEntitiesById,
        IReadOnlyDictionary<Guid, CombatEntity> friendlyTemplatesBySourceEntityId,
        IReadOnlyDictionary<Guid, CombatEntity> hostileTemplatesBySourceEntityId)
    {
        SourceEntitiesById = sourceEntitiesById;
        FriendlyTemplatesBySourceEntityId = friendlyTemplatesBySourceEntityId;
        HostileTemplatesBySourceEntityId = hostileTemplatesBySourceEntityId;
    }

    public IReadOnlyDictionary<Guid, Entity> SourceEntitiesById { get; }

    public IReadOnlyDictionary<Guid, CombatEntity> FriendlyTemplatesBySourceEntityId { get; }

    public IReadOnlyDictionary<Guid, CombatEntity> HostileTemplatesBySourceEntityId { get; }
}
