using Domain.Models.Professions.Crafting;

namespace Domain.Models.CharacterActions;
public interface ICharacterActionRepository
{
    /// <summary>
    /// Start a CharacterAction in relation to the Character's Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Get a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<CharacterAction?> GetActionScheduleAsync(Guid characterId, CancellationToken cancellationToken);
    public Task<CharacterAction?> GetCombatActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken);
    public Task<CharacterAction?> GetCraftingActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken);

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
    public Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Get a character's crafting action
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem characterAction, DateTimeOffset now, CancellationToken cancellationToken);
    Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken);
}
