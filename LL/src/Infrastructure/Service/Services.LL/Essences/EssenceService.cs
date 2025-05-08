using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;

namespace Services.LL.Essences;
public class EssenceService : IEssenceService
{
    private readonly IEssenceRepository _essenceRepository;

    public EssenceService(IEssenceRepository essenceRepository)
    {
        _essenceRepository = essenceRepository;
    }

    public async Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken) =>
        await _essenceRepository.EquipEssence(characterId, essenceItemId, cancellationToken);

    public async Task<EquippedEssencesAndInventoryEssences> GetEquippedEssencesAndInventoryEssences(Guid characterId, CancellationToken cancellationToken) =>
        await _essenceRepository.GetEquippedEssencesAndInventoryEssences(characterId, cancellationToken);

    public async Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken) =>
        await _essenceRepository.DeleteEquippedEssence(characterId, essenceId, cancellationToken);
}