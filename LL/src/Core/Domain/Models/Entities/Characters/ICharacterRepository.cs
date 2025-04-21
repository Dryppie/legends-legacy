
namespace Domain.Models.Entities.Characters;
public interface ICharacterRepository
{
    /// <summary>
    /// Create a Character in relation to the User's Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="Username"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Character> CreateCharacterAsync(string userId, string username, CancellationToken cancellationToken);

    /// <summary>
    /// Get Character by User Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <returns></returns>
    public Task<Character> GetCharacterByUserIdAsync(Guid currentUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Get Character by Character Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <returns></returns>
    public Task<Character> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get Character Overview by Character Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <returns></returns>
    Task<Character> GetCharacterOverviewByCharacterIdAsync(Guid currentUserId, CancellationToken cancellationToken);
    Task<List<CharacterLeaderboardItem>> GetLeaderboardCharactersAsync(CancellationToken cancellationToken);
    Task UpdateCharacterNameAsync(string userId, string username, CancellationToken cancellationToken);
}