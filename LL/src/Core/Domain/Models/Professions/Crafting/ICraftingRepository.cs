using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;
public interface ICraftingRepository
{
    Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken);
}