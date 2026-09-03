using Domain.Models.CharacterActions;

namespace Application.Interfaces.Services.LL.CharacterActions;
public interface ICharacterActionService
{
    /// <summary>
    /// Start a CharacterAction in relation to the Character's Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<CharacterAction?> StartCharacterActionAsync(
        CharacterAction characterAction,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the current action without resolving elapsed work or applying rewards.
    /// </summary>
    public Task<CharacterAction?> PeekCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);

}
