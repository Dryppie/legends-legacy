using Domain.Models.Professions.Crafting;

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
    public void UpdateCharacterAction(CharacterAction characterAction);

    /// <summary>
    /// Delete a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken);

    /// <summary>
    /// Get a character's crafting action
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken);
    Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken);
}