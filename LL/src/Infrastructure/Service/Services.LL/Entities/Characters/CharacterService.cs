using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Domain.Components.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Sets;

namespace Services.LL.Entities.Characters;

public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;
    private readonly ICharacterExperienceProgressionProvider _experienceProgression;
    private readonly ICraftingDefinitionProvider? _craftingDefinitions;

    public CharacterService(
        ICharacterRepository characterRepository,
        IEssenceCombatLoadoutResolver essenceLoadouts,
        ICharacterExperienceProgressionProvider experienceProgression,
        ICraftingDefinitionProvider? craftingDefinitions = null)
    {
        _characterRepository = characterRepository;
        _essenceLoadouts = essenceLoadouts;
        _experienceProgression = experienceProgression;
        _craftingDefinitions = craftingDefinitions;
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
        ApplyCombatAttributes(character);
        return character;
    }

    public async Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterNameAsync(characterName, cancellationToken);
        if (character == null) return null;

        SetExperienceRequirement(character);
        ApplyCombatAttributes(character);
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

    private void ApplyCombatAttributes(Character character)
    {
        var defaultLoadout = EssenceLoadoutSelection.Select(character.EssenceLoadouts, EssenceCombatActivity.None);
        var equippedEssences = defaultLoadout?.Slots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<Domain.Models.Essences.PlayerEssence>()
            .ToList() ?? [];
        var loadout = _essenceLoadouts.Resolve(character.Id, equippedEssences);
        var equipment = character.EquipmentSlots
            .Where(slot => slot.EquipmentInstance is not null)
            .Select(slot => slot.EquipmentInstance!)
            .DistinctBy(item => item.Id)
            .ToArray();
        IReadOnlyList<AttributeModifierBase> setModifiers = _craftingDefinitions is null
            ? Array.Empty<AttributeModifierBase>()
            : EquipmentSetBonusResolver.ResolveAttributeModifiers(
                equipment,
                _craftingDefinitions.GetEquipmentSets());
        AttributeCalculator.CalculateBaseAttributes(
            character,
            loadout.AttributeModifiers.Concat(setModifiers));
    }

    private void SetExperienceRequirement(Character? character)
    {
        if (character is not null)
        {
            character.ExperienceUntilNextLevel = _experienceProgression.GetRequiredExperience(character.Level);
        }
    }
}
