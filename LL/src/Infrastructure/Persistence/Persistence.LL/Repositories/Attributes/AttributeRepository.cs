using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Helpers;
using Domain.Models.Attributes;
using Persistence.LL.Interfaces;

namespace Persistence.LL.Repositories.Attributes;
public class AttributeRepository : IAttributeRepository
{
    private readonly IUnitOfWork _unitOfWork;
    public AttributeRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // inheritdoc />
    public async Task<IEnumerable<EntityAttribute>> CreateAttributesForNewCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        // Create EntityAttribute for each fetched Attribute
        var characterAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(characterId);

        // Add them to the DbContext
        _unitOfWork.Context.EntityAttributes.AddRange(characterAttributes);

        // Save changes to the database
        await _unitOfWork.Context.SaveChangesAsync(cancellationToken);

        return characterAttributes;
    }

    // inheritdoc />
    public IEnumerable<EntityAttribute> GetAttributesByCharacterId(Guid characterId)
    {
        var attributes = _unitOfWork.Context.EntityAttributes.Where(a => a.EntityId.Equals(characterId)).ToList();
        NotFoundException.ThrowIfNull(attributes, nameof(EntityAttribute), characterId);

        return attributes;
    }
}