using Domain.Models.Entities;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed record LoadedEncounterEntities(
    IReadOnlyDictionary<Guid, Entity> SourceEntitiesById);