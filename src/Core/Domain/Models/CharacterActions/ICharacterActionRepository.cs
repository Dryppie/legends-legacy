
namespace Domain.Models.CharacterActions;
public interface ICharacterActionRepository
{
    /// <summary>
    /// Start a CharacterAction in relation to the Character's Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken);

    /// <summary>
    /// Get a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Update a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task UpdateCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);
}