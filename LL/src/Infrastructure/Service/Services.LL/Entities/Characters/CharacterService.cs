using Application.Interfaces.Services.LL;
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
    public async Task<Character> CreateCharacterAsync(string UserId, string Username, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.CreateCharacterAsync(UserId, Username, cancellationToken);

        return character;
    }
    /// <inheritdoc/>
    public async Task<Character> GetMyCharacterAsync(Guid CurrentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterByUserIdAsync(CurrentUserId, cancellationToken);
        character.ExperienceUntilNextLevel = EntityLevelConstants.XP_REQUIRED(character.Level);
        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByCharacterIdAsync(Guid CharacterId, CancellationToken cancellationToken)
    {
        return await _characterRepository.GetCharacterByCharacterIdAsync(CharacterId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Character> GetMyCharacterOverviewAsync(Guid CurrentUserId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterIdAsync(CurrentUserId, cancellationToken);
        AttributeCalculator.CalculateBaseAttributes(character);
        foreach (var essence in character.EssenceSlots.Where(es => es.OccupiedEssence != null).Select(es => es.OccupiedEssence!))
        {
            essence.Active.Description = _essenceDescriptionService.BuildAbilityDescription(essence.Active, character.BaseCombatAttributes);
            essence.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(essence.Passive, character.BaseCombatAttributes);
        }
        return character;
    }

    public async Task<List<CharacterLeaderboardItem>> GetLeaderboardCharactersAsync(CancellationToken cancellationToken)
    {
        return await _characterRepository.GetLeaderboardCharactersAsync(cancellationToken);
    }
}