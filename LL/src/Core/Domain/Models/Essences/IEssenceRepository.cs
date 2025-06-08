using Domain.Models.Essences.EssenceSlots;

namespace Domain.Models.Essences;
public interface IEssenceRepository
{
    Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken);
    Task<List<EssenceSlot>> GetEquippedEssences(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken);
}