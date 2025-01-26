using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Helpers;
using Domain.Models.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Attributes;
public class AttributeRepository : IAttributeRepository
{
    private readonly IDbContext _context;
    public AttributeRepository(IDbContext context)
    {
        _context = context;
    }

    // inheritdoc />
    public async Task<IEnumerable<EntityAttribute>> CreateAttributesForNewCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        // Create EntityAttribute for each fetched Attribute
        var characterAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(characterId);

        // Add them to the DbContext
        _context.EntityAttributes.AddRange(characterAttributes);

        // Save changes to the database
        await _context.SaveChangesAsync(cancellationToken);

        return characterAttributes;
    }

    // inheritdoc />
    public async Task<List<EntityAttribute>> GetAttributesByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var attributes = await _context.EntityAttributes.Where(a => a.EntityId.Equals(characterId)).ToListAsync(cancellationToken);
        NotFoundException.ThrowIfNull(attributes.First(), nameof(EntityAttribute), characterId);

        return attributes;
    }
}