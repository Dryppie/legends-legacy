using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;
public interface IEssenceService
{
    Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken);
    Task<EquippedEssencesAndInventoryEssences> GetEquippedEssencesAndInventoryEssences(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken);
}