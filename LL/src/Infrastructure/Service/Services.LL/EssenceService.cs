using Application.Interfaces.Services.LL;
using Domain.Models.Essences;

namespace Services.LL;
public class EssenceService : IEssenceService
{
    private readonly IEssenceRepository _essenceRepository;

    public EssenceService(IEssenceRepository essenceRepository)
    {
        _essenceRepository = essenceRepository;
    }

    public Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken)
    {
        return _essenceRepository.EquipEssence(characterId, essenceItemId, cancellationToken);
    }

    public async Task<EquippedEssencesAndInventoryEssences> GetEquippedEssencesAndInventoryEssences(Guid characterId, CancellationToken cancellationToken)
    {
        var equippedEssencesAndInventoryEssences = await _essenceRepository.GetEquippedEssencesAndInventoryEssences(characterId, cancellationToken);

        return equippedEssencesAndInventoryEssences;
    }

    public async Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken)
    {
        return await _essenceRepository.DeleteEquippedEssence(characterId, essenceId, cancellationToken);
    }
}