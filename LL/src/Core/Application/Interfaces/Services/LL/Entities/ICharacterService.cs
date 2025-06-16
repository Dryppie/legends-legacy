using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL.Entities;
public interface ICharacterService
{
    /// <summary>
    /// Create a Character in relation to the User's Id
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="username"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken);

    /// <summary>
    /// Get the current User's Character through the User's Id
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <returns></returns>
    public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Get Character by Character Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get Character Overview by Character Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken);
    /// <summary>
    /// Get a bare minimum character, with no includes
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}