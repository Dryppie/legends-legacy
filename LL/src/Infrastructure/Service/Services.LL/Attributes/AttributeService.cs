using Application.Interfaces.Services.LL;
using Domain.Models.Attributes;

namespace Services.LL.Attributes;
public class AttributeService : IAttributeService
{
    private readonly IAttributeRepository _attributesRepository;

    public AttributeService(IAttributeRepository attributesRepository)
    {
        _attributesRepository = attributesRepository;
    }

    public async Task<IEnumerable<EntityAttribute>> CreateAttributesForNewCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _attributesRepository.CreateAttributesForNewCharacterAsync(characterId, cancellationToken);
    }

    public async Task<List<EntityAttribute>> GetAttributesByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _attributesRepository.GetAttributesByCharacterIdAsync(characterId, cancellationToken);
    }
}