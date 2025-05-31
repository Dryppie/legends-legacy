using Domain.Models.CharacterActions;
using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL.CharacterActions;
public interface ICharacterActionService
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
    /// Delete a character's current action
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken);
}