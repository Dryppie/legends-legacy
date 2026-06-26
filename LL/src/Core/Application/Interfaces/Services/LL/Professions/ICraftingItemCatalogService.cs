using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingItemCatalogService
{
    Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken);
}
