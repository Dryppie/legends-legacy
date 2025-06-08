using Domain.Models.Essences.EssenceSlots;

namespace Application.Interfaces.Services.LL.Essences;
public interface IEssenceService
{
    Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken);
    Task<List<EssenceSlot>> GetEquippedEssences(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken);
}