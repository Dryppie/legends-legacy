using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Services.LL.Interfaces;

namespace Services.LL.Essences;
public class EssenceService : IEssenceService
{
    private readonly IEssenceRepository _essenceRepository;
    private readonly IEssenceDescriptionService _essenceDescriptionService;
    private readonly ICharacterService _characterService;
    public EssenceService(IEssenceRepository essenceRepository, IEssenceDescriptionService essenceDescriptionService, ICharacterService characterService)
    {
        _essenceRepository = essenceRepository;
        _essenceDescriptionService = essenceDescriptionService;
        _characterService = characterService;
    }

    public async Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken) =>
        await _essenceRepository.EquipEssence(characterId, essenceItemId, cancellationToken);

    public async Task<List<EssenceSlot>> GetEquippedEssences(Guid characterId, CancellationToken cancellationToken)
    {
        var essenceSlots = await _essenceRepository.GetEquippedEssences(characterId, cancellationToken);
        if (essenceSlots.Count == 0) return [];

        var character = await _characterService.GetMyCharacterOverviewAsync(characterId, cancellationToken); // Called to calculate correct description for abilities (X-Y damage / heal)
        if (character == null) return [];

        foreach (var slot in essenceSlots.Where(es => es.OccupiedEssence != null))
        {
            slot.OccupiedEssence!.Active.Description = _essenceDescriptionService.BuildAbilityDescription(slot.OccupiedEssence.Active, character.BaseCombatAttributes);
            slot.OccupiedEssence!.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(slot.OccupiedEssence.Passive, character.BaseCombatAttributes);
        }
        return essenceSlots;
    }

    public async Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken) =>
        await _essenceRepository.DeleteEquippedEssence(characterId, essenceId, cancellationToken);
}