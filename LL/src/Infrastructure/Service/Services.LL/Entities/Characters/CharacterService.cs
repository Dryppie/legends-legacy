using Application.Interfaces.Services.LL.Entities;
using Domain.Components.Attributes;
using Domain.Helpers.Constants;
using Domain.Models.Entities.Characters;
using Services.LL.Interfaces;

namespace Services.LL.Entities.Characters;
public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IEssenceDescriptionService _essenceDescriptionService;

    public CharacterService(ICharacterRepository characterRepository, IEssenceDescriptionService essenceDescriptionService)
    {
        _characterRepository = characterRepository;
        _essenceDescriptionService = essenceDescriptionService;
    }

    /// <inheritdoc/>
    public async Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.CreateCharacterAsync(userId, username, cancellationToken);

        return character;
    }
    /// <inheritdoc/>
    public async Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterByUserIdAsync(currentUserId, cancellationToken);
        if (character != null) character.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(character.Level);

        return character;
    }

    /// <inheritdoc/>
    public async Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _characterRepository.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Character?> GetMyCharacterOverviewAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterIdAsync(currentUserId, cancellationToken);
        if (character == null) return null;

        AttributeCalculator.CalculateBaseAttributes(character);
        foreach (var essence in character.EssenceSlots.Where(es => es.OccupiedEssence != null).Select(es => es.OccupiedEssence!))
        {
            _essenceDescriptionService.BuildAbilityDescription(essence.Active, character.BaseCombatAttributes);
            _essenceDescriptionService.BuildAbilityDescription(essence.Passive, character.BaseCombatAttributes);
        }
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterNameAsync(characterName, cancellationToken);
        if (character == null) return null;

        AttributeCalculator.CalculateBaseAttributes(character);
        foreach (var essence in character.EssenceSlots.Where(es => es.OccupiedEssence != null).Select(es => es.OccupiedEssence!))
        {
            _essenceDescriptionService.BuildAbilityDescription(essence.Active, character.BaseCombatAttributes);
            _essenceDescriptionService.BuildAbilityDescription(essence.Passive, character.BaseCombatAttributes);
        }
        return character;
    }

    public async Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _characterRepository.GetBaseCharacterByIdAsync(characterId, cancellationToken);

    public async Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) =>
        await _characterRepository.UpdateCharacterNameAsync(userId, username, cancellationToken);

    public async Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _characterRepository.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);

    public async Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) =>
        await _characterRepository.GetCharacterIdByNameAsync(name, cancellationToken);
}