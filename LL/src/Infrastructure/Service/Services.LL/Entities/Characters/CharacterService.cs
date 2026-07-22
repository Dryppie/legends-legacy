using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Entities.Characters;

namespace Services.LL.Entities.Characters;

public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ICombatRatingService _combatRatings;
    private readonly ICharacterExperienceProgressionProvider _experienceProgression;

    public CharacterService(
        ICharacterRepository characterRepository,
        ICombatRatingService combatRatings,
        ICharacterExperienceProgressionProvider experienceProgression)
    {
        _characterRepository = characterRepository;
        _combatRatings = combatRatings;
        _experienceProgression = experienceProgression;
    }

    public async Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.CreateCharacterAsync(userId, username, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterByUserIdAsync(currentUserId, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<Character?> GetMyCharacterOverviewAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterIdAsync(currentUserId, cancellationToken);
        if (character == null) return null;

        SetExperienceRequirement(character);
        ApplyCombatRating(character);
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterNameAsync(characterName, cancellationToken);
        if (character == null) return null;

        SetExperienceRequirement(character);
        ApplyCombatRating(character);
        return character;
    }

    public async Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetBaseCharacterByIdAsync(characterId, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.UpdateCharacterNameAsync(userId, username, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) =>
        await _characterRepository.IsCharacterNameTakenAsync(name, excludedCharacterId, cancellationToken);

    public async Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        SetExperienceRequirement(character);
        return character;
    }

    public async Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) =>
        await _characterRepository.GetCharacterIdByNameAsync(name, cancellationToken);

    public async Task<int> GetCombatRatingAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await GetMyCharacterOverviewAsync(characterId, cancellationToken);
        return character is null
            ? 0
            : character.CombatRating.Total;
    }

    private void ApplyCombatRating(Character character)
    {
        var activeLoadout = character.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        var equippedEssences = activeLoadout?.Slots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<Domain.Models.Essences.PlayerEssence>()
            .ToList() ?? [];
        var equipmentModifiers = character.EquipmentSlots
            .Where(slot => slot.EquipmentInstance is not null)
            .SelectMany(slot => slot.EquipmentInstance!.AttributeModifiers)
            .Cast<Domain.Models.Attributes.Modifiers.AttributeModifierBase>()
            .ToList();
        var projection = _combatRatings.Calculate(
            character.BaseAttributes.ToDictionary(x => x.AttributeType, x => x.Value),
            equipmentModifiers,
            equippedEssences);

        character.BaseCombatAttributes.Clear();
        foreach (var (attribute, value) in projection.Attributes)
            character.BaseCombatAttributes[attribute] = value;
        character.CombatRating = projection.Breakdown;
    }

    private void SetExperienceRequirement(Character? character)
    {
        if (character is not null)
        {
            character.ExperienceUntilNextLevel = _experienceProgression.GetRequiredExperience(character.Level);
        }
    }
}
