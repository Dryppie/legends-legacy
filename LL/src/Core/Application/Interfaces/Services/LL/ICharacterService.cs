using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL;
public interface ICharacterService
{
    /// <summary>
    /// Create a Character in relation to the User's Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <param name="Username"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Character> CreateCharacterAsync(string UserId, string Username, CancellationToken cancellationToken);

    /// <summary>
    /// Get the current User's Character through the User's Id
    /// </summary>
    /// <param name="CurrentUserId"></param>
    /// <returns></returns>
    public Task<Character> GetMyCharacterAsync(Guid CurrentUserId);

    /// <summary>
    /// Get Character by Character Id
    /// </summary>
    /// <param name="UserId"></param>
    /// <returns></returns>
    public Task<Character> GetCharacterByCharacterIdAsync(Guid CharacterId);
}