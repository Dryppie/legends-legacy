using Domain.Models.Attributes;

namespace Application.Interfaces.Services.LL;
public interface IAttributeService
{
    /// <summary>
    /// Create Attributes in relation to the Character's Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public Task<IEnumerable<EntityAttribute>> CreateAttributesForNewCharacterAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get Attributes by Character Id
    /// </summary>
    /// <param name="characterId"></param>
    /// <returns></returns>
    public IEnumerable<EntityAttribute> GetAttributesByCharacterId(Guid characterId);
}