using Application.UseCases.Tutorials.Dtos;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Tutorials;

public interface ITutorialService
{
    Task<TutorialStateDto> GetStateAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TutorialStateDto> RecordCraftingPageVisitedAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> CanStartCombatAreaAsync(Guid characterId, string areaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> RecordIdleCombatAsync(Guid characterId, string areaId, bool wonEncounter, CancellationToken cancellationToken);
    Task RecordEssenceAbsorbedAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken);
    Task RecordEssenceLoadoutChangedAsync(Guid characterId, IReadOnlyCollection<Guid>? attunedPlayerEssenceIds, CancellationToken cancellationToken);
    Task RecordCraftedEquipmentAsync(Guid characterId, IReadOnlyCollection<EquipmentInstance> craftedItems, CancellationToken cancellationToken);
    Task RecordEquipmentChangedAsync(Guid characterId, CancellationToken cancellationToken);
}
