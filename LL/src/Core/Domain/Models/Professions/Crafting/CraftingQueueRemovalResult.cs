using Domain.Models.CharacterActions;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;

public sealed record CraftingQueueRemovalResult(
    CharacterAction? Action,
    IReadOnlyList<EquipmentInstance> EquipmentInstances,
    IReadOnlyList<Guid> RemovedQueueItemIds);
