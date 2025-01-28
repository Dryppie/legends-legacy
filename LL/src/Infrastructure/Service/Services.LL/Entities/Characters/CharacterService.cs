using Application.Interfaces.Services.LL;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Interfaces;
using System.Threading;

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
    public async Task<Character> GetMyCharacterAsync(Guid CurrentUserId)
    {
        var character = await _characterRepository.GetCharacterByUserIdAsync(CurrentUserId);
        //character.CharacterNextLevelCalculator();
        return character;
    }

    /// <inheritdoc/>
    public async Task<Character> GetCharacterByCharacterIdAsync(Guid CharacterId)
    {
        return await _characterRepository.GetCharacterByCharacterIdAsync(CharacterId);
    }

    /// <inheritdoc/>
    public async Task<Character> GetMyCharacterOverviewAsync(Guid CurrentUserId)
    {
        var character = await _characterRepository.GetCharacterOverviewByCharacterIdAsync(CurrentUserId);
        foreach (var essence in character.EquippedEssences)
        {
            essence.Active.Description = _essenceDescriptionService.BuildAbilityDescription(essence.Active, [.. character.BaseAttributes]);
            essence.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(essence.Passive, [.. character.BaseAttributes]);
        }
        //character.CharacterNextLevelCalculator();
        return character;
    }
}