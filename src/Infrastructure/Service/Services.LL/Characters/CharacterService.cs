using Application.Interfaces.Services.LL;
using Domain.Models.Entities.Actors.Characters;

namespace Services.LL.Characters;
public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;

    public CharacterService(ICharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
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
}