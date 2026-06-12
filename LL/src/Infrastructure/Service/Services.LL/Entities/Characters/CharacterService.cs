using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Helpers.Constants;
using Domain.Models.Entities.Characters;

namespace Services.LL.Entities.Characters;

public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IEssenceBonusProvider _essenceBonusProvider;

    public CharacterService(
        ICharacterRepository characterRepository,
        IEssenceBonusProvider essenceBonusProvider)
    {
        _characterRepository = characterRepository;
        _essenceBonusProvider = essenceBonusProvider;
    }

    public async Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) =>
        await _characterRepository.CreateCharacterAsync(userId, username, cancellationToken);

    public async Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterByUserIdAsync(currentUserId, cancellationToken);
        if (character != null) character.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(character.Level);
        return character;
    }

    public async Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _characterRepository.GetCharacterByCharacterIdAsync(characterId, cancellationToken);

    public async Task<Character?> GetMyCharacterOverviewAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterIdAsync(currentUserId, cancellationToken);
        if (character == null) return null;

        var essenceModifiers = GetLoadedEssenceModifiers(character);
        AttributeCalculator.CalculateBaseAttributes(character, essenceModifiers);
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterNameAsync(characterName, cancellationToken);
        if (character == null) return null;

        var essenceModifiers = GetLoadedEssenceModifiers(character);
        AttributeCalculator.CalculateBaseAttributes(character, essenceModifiers);
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

    private IReadOnlyList<Domain.Models.Attributes.Modifiers.AttributeModifierBase> GetLoadedEssenceModifiers(Character character)
    {
        var activeLoadout = character.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        if (activeLoadout is null) return [];

        return _essenceBonusProvider.GetAttunedAttributeModifiers(
            activeLoadout.Slots
                .Select(x => x.PlayerEssence)
                .Where(x => x is not null)
                .Cast<Domain.Models.Essences.PlayerEssence>());
    }
}
